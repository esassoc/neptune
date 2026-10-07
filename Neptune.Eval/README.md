# Neptune.Eval: WQMP AI extraction eval (NPT-1132)

Measures how accurately WQMP AI extraction fills the review wizard, so changes to the prompts, schemas or model are judged by numbers rather than by eye.

- **Ground truth:** WQMPs whose fields, parcels and BMPs were entered by hand. The extraction is scored against those saved values.
- **What runs:** the production `WqmpExtractionService` itself, called the same way the `/extract` endpoint calls it.
- **Results:** saved to `runs/`, not to `WaterQualityManagementPlanExtractionResult`, so local review-wizard state is untouched.

## Running

Run from the repo root inside the devcontainer, which already has the database, blob and Key Vault configuration. `--artifacts-path` keeps this build out of the `bin`/`obj` folders that `make api`'s `dotnet watch` is using.

```bash
# Baseline on the dev split (spends Anthropic credits; prints the selection and stops without --yes)
dotnet run --project Neptune.Eval --artifacts-path /tmp/neptune-eval-artifacts -- run --split dev --label baseline --yes

# A couple of documents first, to check the setup
dotnet run --project Neptune.Eval --artifacts-path /tmp/neptune-eval-artifacts -- run --ids 1234,5678 --label smoke --yes

# Re-score a saved run after changing the scorer (free)
dotnet run --project Neptune.Eval --artifacts-path /tmp/neptune-eval-artifacts -- score Neptune.Eval/runs/<run>
```

`run` options:

| Option | Effect |
|---|---|
| `--split dev\|test\|all` | Which part of the eval set to run. Defaults to `dev`. |
| `--ids` | Run specific WQMPs instead of a split. |
| `--limit N` | Run only the first N documents. |
| `--model claude-…` | Override `ClaudeModelId`. Works with Sonnet 5.5 / Opus 5.5: the service uses `tool_choice: auto`, which they require. |
| `--effort low\|medium\|high\|xhigh\|max` | Override `ClaudeEffort` (unset = the model's default). Levels accepted vary by model. |
| `--max-cost 25` | Stop starting new documents once the run has spent this many dollars (documents in flight finish). |
| `--concurrency N` | Documents in flight at once. Defaults to 2. |
| `--label name` | Name used in the run folder and scorecard title. |

**Cost.** Every `run` spends credits on the Anthropic key that Dev, QA and Prod share, and counts against the organization's monthly spend limit. On 2026-10-07 eval runs exhausted that limit and took AI extraction down in **every environment**. Before a run:
- Check the remaining headroom in the Anthropic Console.
- Use the smallest document set that answers the question (`--ids`, `--limit`).
- Always pass `--max-cost`.

An Anthropic account failure (credits, usage limit, key) stops the run, and those documents are reported as not scored rather than as misses. Batch mode isn't offered: the four category calls share one cached copy of the PDF, and cache hits inside a batch are only best-effort, so batching can cost more, not less.

## Eval set (`eval-set.json`)

40 WQMPs, chosen by `select` (seeded, so it's reproducible). Candidates are WQMPs with a Final WQMP PDF, at least 12 of 17 hand-entered fields, and parcels.

| PDF type | Docs | Definition |
|---|---|---|
| scanned | 20 | ≥ 80% of pages have no text layer |
| mixed | 8 | 20–79% of pages have no text layer |
| digital | 12 | < 20% of pages have no text layer |

- **Composition:** weighted toward scanned documents, which are most of the real corpus. At least 15 have all four categories (fields, parcels, QuickBMPs, source control BMPs). At most 8 come from any one jurisdiction, and PDFs over 550 pages are excluded (Anthropic's limit is 600).
- **Split:** ~60% `dev`, used to iterate. ~40% `test`, held out and only run to confirm a final choice. Tuning against `test` defeats the point.

`select` overwrites the checked-in set, so only rerun it to change the set on purpose. It downloads candidate PDFs to classify them, but makes no Anthropic calls.

## Scoring

The scorer applies extracted values the way the review wizard does (`wqmp-review.component.ts` `makeField`):

- **Dropdowns:** count only when the extracted label matches an option label (case-insensitive). Otherwise the wizard leaves the field blank, so it's scored **Missed (unmapped)**.
- **Dates:** parsed. **Acres:** rounded to 2 decimals (±0.01). **Phone:** last 10 digits. **ZIP:** first 5 digits. **State:** 2-letter code or full name.
- **Trash capture status:** "No Trash Capture" and "Not Provided" count as the same answer (no trash capture device in the plan). The records use them interchangeably.
- **Text** (contact, address, record number): compared ignoring case and punctuation. **Close** means one contains the other or they share most words; a reviewer would tweak it rather than retype it.

**18 fields are scored:** the wizard's location and basics fields, minus Jurisdiction and WQMP Name (entered at upload), Modeling Approach and Trash Capture Effectiveness (not extracted).

| Outcome | Meaning |
|---|---|
| Correct / Close / Wrong | The WQMP has a value, and the extraction got it right / nearly right / wrong. |
| Missed | The WQMP has a value; the extraction gave none, or one the wizard can't use. |
| Extra | The WQMP has no value but the extraction produced one. Not scored: the hand-entered data may just be incomplete. |

**Field accuracy** = Correct ÷ (Correct + Close + Wrong + Missed). **Lenient** also counts Close.

**Info-only fields** (`EvalFields.InfoOnly`) are reported in the per-field table but excluded from accuracy. The hand-entered data doesn't follow one convention for them, so a disagreement often isn't an extraction error:
- **Record Number** holds the WQMP number, a grading permit number, or a project name.
- **The maintenance contact address** (Address 1/2, City, State, ZIP) is sometimes the site and sometimes the contact's mailing address.
- **Priority:** Neptune's High / Low may not be the document's "Priority / Non-Priority Project" designation. WQMP 3239's cover says Non-Priority, but its record says High.

Revisit these once the convention is settled.

**List categories** are scored as precision / recall against the WQMP's records. A category is **not scored** (shown as `–`) when the WQMP has no records for it, since data entry may just never have covered it. Parcels are only scored when the WQMP has 1–10: larger lists usually come from parcel splits after the plan was written. For parcels, only **precision** counts. Recall is shown in parentheses for information: records often list APNs the plan never mentions (WQMP 2760's three APNs appear nowhere in its text), so a "missed" parcel usually isn't an extraction error.

| Category | Matched on |
|---|---|
| Parcels | APN, exact string after trimming (the wizard's lookup is an exact match) |
| QuickBMPs | Treatment BMP type, as a multiset (names are too inconsistent between PDF and data entry to match on) |
| Source control BMPs | Attributes marked present (the wizard matches attribute names exactly, case-insensitive) |

**Hiccups** come from the extraction service:
- empty or invalid output, or the wrong tool called
- `max_tokens` stops
- a list category without an `items` array
- WQMP fields that fail the output check (missing, malformed, bad bounding box); the WQMP tool is the one non-strict tool, so `ValidateWqmpOutput` checks it in code
- stale `file_id` re-uploads

**Cost** uses `Pricing.cs` list prices, including cache writes.

## Output

`runs/<yyyyMMdd-HHmm>-<label>/`. It's gitignored because it contains extracted WQMP contents.

| File | Contents |
|---|---|
| `scorecard.md` | Headline by PDF type, per-field table, hiccups, per-document table |
| `fields.csv` | Every field comparison (expected vs extracted), for digging into misses |
| `docs/<WQMP id>.json` | Raw extraction output, usage per call, hiccups |
| `run.json` | Run metadata |
