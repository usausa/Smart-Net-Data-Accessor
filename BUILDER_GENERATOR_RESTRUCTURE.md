# Builder Generator 構造見直し方針

対象: `Smart.Data.Accessor.Builders.Generator`（ANSI / SqlServer / MySql / Postgres の QueryBuilder ジェネレータ群）。

**実施済み（2026-06-05）。** Q1=完全複製 / Q2=ANSI も 1 provider / Q3=案 A / Q4=Phase 1 先行 / Q5=作り替えのみ で合意し、§9 のとおり完了。残作業は §9.3 の追加タスク一覧を参照。

---

## 1. 背景・目的

- 現状、全プロバイダ（ANSI/SqlServer/MySql/Postgres）が `QueryBuilderEngine` の**単一パイプライン**を共有し、差分は **`SqlDialect`（識別子クォート＋ページング句）／QueryBuilder 属性 FQN／`providerTag`（ファイル名）だけ**。
- 将来はプロバイダ毎に**異なる処理**が必要になる想定（例: SQL Server の `MERGE`/UPSERT・`OUTPUT`、PostgreSQL の `INSERT ... ON CONFLICT`/`RETURNING`、provider 固有のページング・型・bulk 操作 等）。
- 現状は「dialect でパラメータ化した 1 エンジン」＝**過剰共通化**。種別（`BuilderKind`）が共有・閉じているため、**プロバイダ固有の種別追加・処理差し替えが shared コードの改変を強いる**。

### ゴール

各プロバイダ ジェネレータが**自分のパイプライン（transform＋出力）と種別を所有**し、共有部品（symbol 抽出・SQL 組立・出力）を**組み合わせて**使う構造へ。プロバイダの差分追加で shared を触らずに済むようにする。

---

## 2. 現状アーキテクチャ

```
[Provider Generator] (×4: QueryBuilderGenerator / SqlServer.. / MySql.. / Postgres..)
  ├─ Targets: (attribute FQN, QueryBuilderEngine.BuilderKind)[]   ← 属性→共有Kind の対応表
  ├─ Dialect: SqlDialect (Ansi/SqlServer/MySql/Postgres)          ← 唯一の実差分
  └─ Initialize → QueryBuilderEngine.Register(context, Targets, Dialect, providerTag)
                                   │
QueryBuilderEngine (Engine/)       │  ← 共有・単一エンジン
  ├─ enum BuilderKind { Insert, Update, Delete, Count, Select, SelectSingle, Truncate }  ← 共有・閉じた列挙
  ├─ Register(): FAWMN([DataAccessor]) → BuilderModelBuilder.Build(ctx, targets) を配線
  │              → RegisterSourceOutput → Emit()
  └─ Emit(): 診断報告 → BuilderSourceBuilder.Build(model, dialect) → AddSource("{ns}_{Class}.QueryBuilders{tag}.g.cs")

BuilderModelBuilder (Builders/, transform・共有)
  ├─ Build(ctx, targets): メソッド走査 → 属性を targets と突合し Kind を決定 → BuildMethod()
  └─ BuildMethod(... BuilderKind kind ...): switch(kind) で InsertModel/UpdateModel/… を生成   ← 共有 switch（:179-230）
       （テーブル名・値パラメータ・エンティティ列の解決は共通。Kind 毎に診断 NoKeyForBuilder 等）

BuilderSourceBuilder (Engine/, emit・共有)
  └─ EmitMethod(): switch(method) 型分岐 → EmitInsert/EmitUpdate/EmitDelete/EmitSelect/...   ← 共有 type-switch（:74-97）
       （SQL 文字列を組立。プロバイダ差分は dialect.Quote / dialect.AppendPaging のみ）

Models (Models/, 共有 equatable): BuilderClassModel / BuilderMethodModel(+7 subtypes) / BuilderColumn / BuilderValueParam
SqlDialect (Engine/, 抽象) + Ansi/SqlServer/MySql/Postgres 実装        ← Quote / AppendPaging
BuilderDiagnostics (Engine/, 共有 SDA1001-1006) / BuilderMapping(MappingResolver, 共有)
```

### 処理フロー（1 アクセサ）

`[DataAccessor]` クラス → 各プロバイダ Generator が FAWMN で受け取り → 自分の `Targets` 属性が付いたメソッドだけ抽出 → 共有 `BuildMethod` で `BuilderKind` 毎の Model 化 → 共有 `BuilderSourceBuilder` が `dialect` 差し込みで C# 生成 → `AddSource`。

---

## 3. 過剰共通化の所在（結合点）

| # | 箇所 | 内容 | 影響 |
| --- | --- | --- | --- |
| C1 | `QueryBuilderEngine.BuilderKind` | 7 種別の**共有・閉じた enum** | provider 固有種別（UPSERT 等）を足すと共有 enum を改変。全 provider に波及 |
| C2 | `BuilderModelBuilder.BuildMethod` の `switch(kind)`（:179-230） | Kind→Model 生成と Kind 別診断が**共有** | 種別追加・provider 別の生成差分が共有 switch を肥大化 |
| C3 | `BuilderSourceBuilder.EmitMethod` の `switch(method)`（:74-97）＋ `EmitInsert` 等 | SQL 組立が**共有**、差分は `dialect` のみ | provider 別の SQL 構造差分（RETURNING/ON CONFLICT/MERGE）を表現できない |
| C4 | `QueryBuilderEngine.Register/Emit` | 配線・出力が**単一**でパラメータ（targets/dialect/tag）注入 | provider は「パラメータを渡すだけ」で、pipeline を所有しない |
| C5 | Model subtypes（InsertModel 等）＋ `Targets` の `(attr, Kind)` | 属性は**共有 Kind の別名**にすぎない | provider 固有の意味付けができない |

→ **実差分は dialect・属性 FQN・providerTag のみ**で、それ以外は全共有。「将来 provider 毎に異なる処理」を入れる余地が構造的に無い。

---

## 4. 見直し方針（ユーザー提案の実現可否評価）

> 結論: **4 点とも実現可能**。かつ「将来 provider が分岐する」前提では妥当な方向。共有を「単一エンジン」から「**部品ライブラリ＋各 provider のコンポジション**」へ作り替える。

| ユーザー提案 | 可否 | 実現方法 |
| --- | --- | --- |
| **QueryBuilderEngine を分割する** | ✅ | 単一 `Register/Emit` を、再利用可能な小さな関数群（symbol 抽出・SQL 組立・出力スキャフォールド）へ分解。`QueryBuilderEngine` という「神クラス」を解体。 |
| **各 IIncrementalGenerator が transform と Execute の入り口まで個別実装** | ✅ | 各 provider Generator が自分で `ForAttributeWithMetadataName` → 自前 transform → `RegisterSourceOutput` → 自前 emit を**配線**。`Register()` への丸投げをやめる。 |
| **分割した機能を各 generator で使う** | ✅ | provider transform/emit の中から共有部品（`MethodResolver`/`SqlEmit`/`BuilderOutput` 等）を呼ぶ。 |
| **BuilderKind を共通定義せず provider 毎に判断・定義** | ✅ | 共有 enum を撤去。各 provider が自分の種別（enum もしくは直接 Model 型）と「属性→種別→生成/出力」の対応を**自前で持つ**。 |

---

## 5. 分割後の構造案

### 5.1 共有「部品ライブラリ」（`QueryBuilderEngine` の分割先）

provider 非依存・kind 非依存の再利用部品にする（linked source のまま）。

- **`MethodResolver`**（旧 `BuildMethod` の共通前半）: メソッド symbol から **テーブル名・エンティティ型・値パラメータ・エンティティ列・[TypeMap]/profile** を解決し、provider 中立の `ResolvedMethod`（equatable 素材）を返す。Kind 判定や Kind 別診断は**含めない**。
- **`SqlEmit` プリミティブ**（旧 `BuilderSourceBuilder` の部品化）: `Quote` 経由の INSERT/UPDATE/WHERE/列リスト/ページング句の組立、`EmitColumnParameter`/`EmitValueParamBinding`/`EmitCommandText`、`ref BuilderContext` シグネチャ出力。**SQL 構造の最小単位**を提供（種別の組み立ては provider 側）。
- **`BuilderOutput`**（旧 `Build` の外殻＋`Emit`）: partial クラス スキャフォールド、メソッド列挙、診断報告、`AddSource`＋ファイル名規約。
- **共有 equatable モデル**: `BuilderClassModel` / `BuilderMethodModel`(base) / `BuilderColumn` / `BuilderValueParam`。**標準種別の subtypes（InsertModel 等）は「標準ライブラリ」として残し、provider が再利用 or 自前追加**（§8 Q1）。
- **`BuilderDiagnostics`**: 共有の汎用診断（partial 必須・重複等）。**provider 固有診断は provider 側で追加**。
- **`SqlDialect`**: 現状維持（Quote/AppendPaging）。provider 固有メンバの追加余地はここにも残す。

### 5.2 各 provider Generator が所有するもの

```
SqlServerQueryBuilderGenerator : IIncrementalGenerator
  ├─ 自前の種別定義（enum Kind もしくは属性→Model 型の対応）            ← C1/C5 を provider 化
  ├─ Initialize():
  │     context.SyntaxProvider.ForAttributeWithMetadataName("[DataAccessor]", ...)
  │       .Select( (ctx,ct) => Transform(ctx, ct) )                     ← 自前 transform（入り口を個別実装）
  │     context.RegisterSourceOutput(models, (spc, m) => Execute(spc, m))   ← 自前 Execute（入り口を個別実装）
  ├─ Transform(ctx, ct):  MethodResolver で素材抽出 → 自前 Kind 判定 → 自前 Model 構築（共有/独自）
  └─ Execute(spc, model): BuilderOutput でスキャフォールド → 自前 dispatch → SqlEmit で SQL 組立
        （標準種別は共有 emit 関数を呼ぶ、provider 固有種別は自前 emit）
```

- 4 generator は当面ほぼ同形（標準種別を共有部品で実装）だが、**分岐時は自分の Transform/Execute/Kind だけ触れば済む**（shared 不変）。
- `SqlServer` が `Upsert` を足す例: `enum Kind` に `Upsert` 追加 → Transform に `UpsertModel` 構築 → Execute に `EmitMerge` 実装。**他 provider・共有部品は無改変**。

### 5.3 BuilderKind の扱い

- 共有 `BuilderKind` enum は**撤去**。
- 各 provider が「属性 → 種別 → (transform 生成, emit 出力)」を自前で持つ。表現は 2 案（§8 Q3）:
  - **案 A: provider 毎に enum＋switch**（最大柔軟・boilerplate 多）。ユーザー提案に最も忠実。
  - **案 B: 属性→`(buildFn, emitFn)` のデリゲート表**（data-driven・記述量小・分岐も差し替えで対応）。
- いずれも「標準種別」は共有関数を指す/呼ぶだけなので、**当面の重複は最小**に保てる。

---

## 6. 段階的移行（挙動不変で進める）

1. **Phase 1: 共有部品の抽出（リファクタのみ・挙動不変）**
   `BuildMethod` の共通前半 → `MethodResolver`、`BuilderSourceBuilder` → `SqlEmit` プリミティブ、`Build/Emit` 外殻 → `BuilderOutput` に切り出す。`QueryBuilderEngine.Register` は内部でこれらを呼ぶ形に。**この時点で全テスト green を維持**。
2. **Phase 2: provider 毎に pipeline 内製化**
   各 generator の `Initialize` を `Register` 丸投げから、自前 FAWMN＋自前 Transform＋自前 RegisterSourceOutput＋自前 Execute に置換（中身は共有部品＋標準種別 dispatch）。1 provider ずつ移行し都度検証。
3. **Phase 3: 共有 BuilderKind / 共有 switch の撤去**
   provider が自前 Kind を持った段階で、`QueryBuilderEngine.BuilderKind` と共有 `switch` を削除。`QueryBuilderEngine` は（残れば）薄い配線ヘルパーのみ、または解体。
4. **検証**: 各 Phase で clean build 0/0 ＋ Generator.Tests / Smart.Data.Accessor.Tests green。生成出力スナップショット（`BuilderSourceBuilderTests`/`ProviderBuilderTests`）で**出力不変**を確認。

---

## 7. トレードオフ・リスク

**利点**
- provider 固有処理（新種別・異なる SQL 構造・固有診断）を**共有コード無改変**で追加可能。「将来分岐」の前提に構造が一致。
- 「神クラス `QueryBuilderEngine`」解体で責務が明確化。共有部品は純粋な再利用ライブラリに。

**コスト・リスク**
- **当面は重複が増える**（4 provider がほぼ同じ pipeline 配線を持つ）。現状の「1 エンジン共有」が消える。→ 標準種別を共有部品＋デリゲート表（案 B）で実装すれば重複は最小化可能。
- 各 provider transform が**等価モデルを返す制約**（インクリメンタルキャッシュ維持）を全 generator で守る必要（`EquatableArray` 等）。`BuilderIncrementalCacheTests` で担保。
- provider 間の**ドリフト**（同じはずの処理が分岐して食い違う）リスク。ただし「分岐させたい」が目的なので許容範囲。共有すべき標準種別は共有部品に集約して防ぐ。
- 移行中の**出力非互換**リスク。Phase 毎スナップショット検証で抑止。

---

## 8. 確認事項（着手前）

- **Q1. 標準種別の Model/emit**: Insert/Update/Delete/Count/Select/SelectSingle/Truncate の Model 型と emit 関数は**共有ライブラリに残し provider が再利用**（中間案・推奨）か、**完全に provider 毎に複製**（最大独立）か。
- **Q2. ANSI(`QueryBuilderGenerator`) の位置づけ**: 標準種別の**参照実装**として共有ライブラリ側に残すか、ANSI も 1 provider として同様に内製化するか。
- **Q3. Kind 表現**: §5.3 の**案 A（provider 毎 enum＋switch）** と **案 B（属性→デリゲート表）** のどちらを基本とするか。
- **Q4. 移行手順**: §6 の Phase 1（共有部品抽出を先に・挙動不変）→ Phase 2/3 の順で良いか。
- **Q5. 範囲**: 今回は**構造の作り替えのみ**（実際の provider 固有処理＝MERGE 等の追加は別タスク）で良いか。

---

### まとめ

ユーザー提案（QueryBuilderEngine 分割／各 generator が transform・Execute を個別実装／分割機能を利用／BuilderKind を provider 毎に定義）は **すべて実現可能**で、「将来 provider 毎に処理が分岐する」前提に対して妥当。鍵は **共有を『単一エンジン』から『部品ライブラリ＋各 provider のコンポジション』へ作り替える**こと。Q1-Q5 を確定後、§6 の段階移行で挙動を保ったまま実施できます。

---

## 9. 実施結果（2026-06-05 完了）

### 9.1 最終構造

```
Engine/ （この generator アセンブリ内で全 provider が共有する部品）
  ├─ MethodResolver + ResolvedMethod   : symbol → 種別非依存の共通データ（テーブル名/値パラメータ/エンティティ列/[TypeMap]）
  ├─ SqlEmit                           : SQL 組立プリミティブ（OpenMethod/EmitCommandText/EmitColumnParameter/EmitValueParamBinding/BindParams/Marker）
  ├─ BuilderClassScanner<TKind> + MethodBuildContext<TKind> : 汎用クラス走査（partial/重複検査・[TypeMap]/profile・等価モデル組立）＋ provider コールバック
  ├─ BuilderOutput                     : 出力スキャフォールド（partial class・診断報告・AddSource・ファイル名規約）
  ├─ SqlDialect（抽象）/ BuilderDiagnostics（SDA1001-1006）/ BuilderMapping
  └─ Models/ : BuilderClassModel / BuilderMethodModel(base のみ) / BuilderColumn / BuilderValueParam

各 provider（ANSI=ルート, SqlServer/MySql/Postgres=Providers/）が自前で所有:
  ├─ {Provider}Kind enum                         （共有の閉じた enum は撤去）
  ├─ {Provider}{Insert..Truncate}Model           （base から派生・完全複製）
  ├─ {Provider}Dialect                           （Quote / AppendPaging）
  └─ {Provider}QueryBuilderGenerator
        ├─ Initialize : FAWMN([DataAccessor]) → BuilderClassScanner.Scan(Targets, BuildMethod) → WithTrackingName → RegisterSourceOutput(BuilderOutput.Emit(EmitMethod, tag))
        ├─ Transform  : BuildMethod(MethodBuildContext<{Provider}Kind>) … MethodResolver.Resolve → switch(kind) → 自前 Model ＋種別診断
        └─ Execute    : EmitMethod(BuilderMethodModel) … SqlEmit.OpenMethod → switch(model) → 自前 Emit{Kind} → SqlEmit.CloseMethod
```

### 9.2 削除したもの

- `Engine/QueryBuilderEngine.cs`（神クラス・共有 `BuilderKind` enum）
- `Builders/BuilderModelBuilder.cs`（共有 transform・共有 `switch(kind)`）
- `Engine/BuilderSourceBuilder.cs`（共有 emit・共有 `switch(method)`）
- `Models/BuilderMethodModel.cs` の 7 subtypes（InsertModel 等）。base のみ残置
- `Smart.Data.Accessor.Generator.Tests/BuilderSourceBuilderTests.cs`（共有 pure-emit のユニットテスト）→ emit が provider 内 private 化したため、ANSI provider 経由の end-to-end `AnsiQueryBuilderTests.cs`（同 9 形状）へ置換

検証: clean build 0 warning / 0 error、Generator.Tests 130 green、Smart.Data.Accessor.Tests 67 green。挙動・生成出力は不変（ProviderBuilderTests / AnsiQueryBuilderTests / BuilderIncrementalCacheTests で担保）。

### 9.3 追加タスク一覧（この作り替えで可能になった provider 固有処理・整備）

**A. provider 固有機能（各 provider の Kind/Model/Transform/Execute だけで完結、shared 無改変）** — 2026-06-05 実装
- A1. ✅ SQL Server: `MERGE` ベースの UPSERT。属性は **SQL 句名に合わせ `[SqlMerge]`**（内部は `SqlServerKind.Merge`／`SqlServerMergeModel`／`EmitMerge`）。`[Key]` で突合、非キー列を UPDATE、無ければ INSERT
- A2. ✅ SQL Server: `OUTPUT` 句。`SqlInsert/Update/Delete` の **`Output` プロパティ**（INSERTED/DELETED 列を返す）。修飾子なので属性プロパティ＝provider transform が属性から直接読む
- A3. ✅ MySQL: `[MySqlUpsert]`（INSERT ... ON DUPLICATE KEY UPDATE）／`[MySqlInsertIgnore]`（INSERT IGNORE）／`[MySqlReplace]`（REPLACE INTO）
- A4. ⏸ **見送り**: bulk insert（複数行 VALUES）。実行時動的 SQL となり spec §8.2「1 メソッド=1 静的 SQL 形状」と衝突するためユーザー判断で未実装
- A5. ✅ PostgreSQL: `[PgUpsert]`（INSERT ... ON CONFLICT (key) DO UPDATE SET col = EXCLUDED.col / 更新無しは DO NOTHING）
- A6. ✅ PostgreSQL: `RETURNING` 句。`PgInsert/Update/Delete` の **`Returning` プロパティ**
- A7. ✕ **クローズ**: provider 固有ページング/型。paging は dialect 毎に実装済み。ORDER BY＝横断的で §8.2 関連、配列型＝型システム拡張のため A 系では非対応（必要時は個別タスク）
- 付随: 属性名の短縮 — **SQL Server は `Sql*`**（`SqlInsert`…`SqlTruncate`／`SqlMerge`）、**PostgreSQL は `Pg*`**（`PgInsert`…`PgTruncate`／`PgUpsert`）。namespace（`...Attributes.SqlServer` / `...Attributes.Postgres`）と内部 generator/Kind/Model（`SqlServer*` / `Postgres*`）は不変。MySQL は `MySql*` のまま
- 検証: 各機能に `ProviderBuilderTests` を追加。clean build 0/0、Generator.Tests 137 / Tests 67 green

**B. 構造・整備（作り替えに伴う方針確定／保守）**
- B1. provider 別 emit/Model は標準形状を 4× 完全複製している（意図的）。標準形状を同期させ続けるか、ドリフトを許容するかの運用方針を明文化
- B2. without-entity の退避分岐（`SELECT *` / `UPDATE SET ` stub）は SDA1004 を出しつつ生成する。正規シナリオか否か・ハードエラー化の是非を再検討
- B3. provider 固有診断を足す際の ID 採番（現状 `BuilderDiagnostics` SDA1xxx は共有）。provider 毎のサブ帯を切るか検討
- B4. provider 固有解決（例: ON CONFLICT のターゲット列読取）が必要になったら `ResolvedMethod` 拡張 or provider 別解決へ。共有/分岐の線引きを決める
- B5. model 型名は provider 接頭辞（`AnsiInsertModel`…）＋共有 base の方針。長期の命名規約として確定

→ **B1〜B5 は §10 で方針確定（2026-06-05）。**

**C. テスト整備** — 2026-06-05 完了
- C1. ✅ 非既定 3 provider の per-kind end-to-end を `ProviderKindBuilderTests`（`[Theory]`×3 provider）で追加: Update / Delete / SelectSingle / Count / Truncate。各 dialect のクォートが全種別に適用されることを確認＝**B1 ドリフトガード兼用**
- C2. ✅ 各 provider 固有種別（A1/A2/A3/A5/A6）に `ProviderBuilderTests` を追加済み
- C3. ✅ without-entity 退避分岐（`SELECT *` / `UPDATE … SET ` stub）を 3 provider 分 `ProviderKindBuilderTests` でカバー
- 検証: Generator.Tests **158**（C1/C3 で +21）／ Tests 67 green、clean build 0/0

> A7 スピンオフ（ORDER BY プロパティ / PG 配列型）は**不要**（ユーザー判断 2026-06-05）。

---

## 10. 方針確定（§9.3 B群の処理・2026-06-05）

- **B1（完全複製の同期 vs ドリフト）**: 4 provider の標準種別（Insert/Update/Delete/Count/Select/SelectSingle/Truncate）の SQL 形状は現状すべて同一。**方針＝provider が固有要件で分岐するまでは同一形状を維持**（複製は将来分岐のための意図的なもの）。ドリフトはレビューで検出。機械的に担保するなら C1（per-provider per-kind テスト）で標準種別の生成出力一致を assert するのが安価なガード。
- **B2（without-entity の扱い）**: 列・キーが要る種別（Select/SelectSingle/Update/Merge/Upsert）で `typeof(T)` 省略時は **`SDA1004`=Error で既にハードエラー**（コンパイル失敗）。`EmitXxx` の退避出力（`SELECT *` / `UPDATE … SET ` 等）はエラー経路の防御で、`SDA1004` によりコンパイルが止まるため実害なし＝**現状維持**。付随して、`SDA1004`/`SDA1005` のタイトル・メッセージが Select 系のみ言及していたのを、対象種別の拡大（Update/Merge/Upsert）に合わせ**種別非依存の文言へ一般化**（`BuilderDiagnostics` ＋ AnalyzerReleases.Unshipped を同期）。
- **B3（provider 固有診断の ID 採番）**: **共有 `BuilderDiagnostics` の単一 `SDA1xxx` プールを連番で確保**（次は SDA1007）。`SDA1xxx` 帯が既に「Builder generator 由来」を示し、メッセージが provider・種別を識別するため **provider 別サブ帯は設けない**。固有診断が必要になったら同プールに追記し、4 つの AnalyzerReleases を同時更新。
- **B4（provider 固有解決の線引き）**: **解決済み（A2/A6 で実証）**。共有 `MethodResolver` は種別非依存の共通データ（table / value params / entity columns / [TypeMap]）のみ解決。provider 固有の属性プロパティ（`Output` / `Returning` 等）は各 provider の transform が `c.Attr` から直読し自前 Model に載せる。**複数 provider が同じ新規共通データを要する場合に限り `ResolvedMethod` を拡張**（単一 provider 専用は provider 側に留める）。
- **B5（Model 命名規約）**: **確定**。種別 Model は **provider 接頭辞＋共有 base**（`{Provider}XxxModel : BuilderMethodModel`、例 `AnsiInsertModel`/`SqlServerMergeModel`/`PostgresUpsertModel`）。属性は短縮接頭辞（`Sql*` / `Pg*` / `MySql*` / コアは無印）。Kind enum は `{Provider}Kind`。いずれも generator アセンブリ内 internal。

検証: 本節のコード変更は `SDA1004`/`SDA1005` 文言一般化のみ（テストは ID assert で不変）。clean build 0/0、Generator.Tests 137 / Tests 67 green。
