/plan

Before writing code, inspect the repository and do an architecture-first planning pass.

For this first response:
1. Summarize the current relevant code and where the draft model/decoder types live.
2. Propose the target file/folder layout.
3. List the concrete types/interfaces you want to introduce or replace.
4. Identify ambiguities, risks, and likely migration pain points.
5. State whether any existing draft types should be removed from compilation or replaced instead of patched in place.
6. Explain the cache decision you recommend for this app.
7. Do not generate code yet.

Additional constraints:
- Treat the current draft model types as disposable unless they are already used elsewhere in the repository in a way that would make replacement risky.
- Prefer replacement over in-place patching if the current design is structurally wrong.
- Do not implement CSV export in this pass.
- Do not add WPF/ViewModel concerns in this pass.
- Keep the Garmin SDK behind a decoder boundary and do not allow SDK types to leak into the domain model.
- The decoding boundary, decoder implementation, and application model/domain types created in this step must go into the `FitToCsvConverter.Data` assembly/project.
- The CSV exporter itself is out of scope for this step, but any export-related metadata added to the model should also live in `FitToCsvConverter.Data`.

You are designing the application model layer and FIT decoding boundary for a WPF/MVVM .NET application that imports Garmin/other FIT activity files with the official Garmin FIT C# SDK and later exports selected parts of the decoded activity tree to CSV.

Favor correctness, immutability of original decoded data, and future edit/reset support over minimizing type count.

PRIMARY GOAL

Design a clean, production-oriented domain model and decoding layer that:

1. decodes a FIT activity file into a strongly modeled in-memory tree,
2. preserves immutable raw/original metadata and values,
3. allows later editing of decoded/presentation values without mutating immutable original state,
4. supports later CSV export at different tree granularities,
5. optionally supports safe in-process caching for repeated imports of the same file content during one application session.

DOMAIN / FIT HIERARCHY REQUIREMENTS

Use Garmin FIT activity-file semantics as the source of truth:

- Root is an Activity
- An activity contains one or more Sessions
- A Session contains Laps
- A Session contains Records
- Developer/custom/unknown fields may exist on Activity, Session, Lap, and Record nodes where applicable
- The design must preserve FIT semantics faithfully, but the business/domain model must not directly depend on Garmin SDK runtime types

NON-GOALS FOR THIS STEP

Do NOT implement the CSV exporter in this step.
Do NOT implement ViewModel concerns in this step.

Specifically do not add:
- INotifyPropertyChanged
- ObservableCollection
- WPF binding logic
- IEditableObject
- undo stack implementation
- DataGrid transaction behavior
- CSV writing/export services

However, the code produced in this step MUST be designed so that a later export step can use the same model without requiring redesign of the decoding model.

ARCHITECTURAL BOUNDARY

The Garmin SDK must be completely decoupled from business/domain code.

Important refinement:
- Do NOT inject IFitFileDecoder into FitFileDecoder itself
- Instead define a domain-facing abstraction such as IFitActivityDecoder
- Provide a concrete implementation such as GarminFitActivityDecoder that depends on the Garmin SDK
- Inject IFitActivityDecoder into the consuming application service / ViewModel / orchestrator
- No Garmin SDK types may cross the decoder boundary into the domain model

Assume the application is relatively small. Prefer testability, readability, and replaceability over patterns like Singleton.

CLEAN CODE / SOLID

Follow common Clean Code and SOLID principles.
Do not cram decoding, caching, export preparation, and later exporting into one type.
Favor small focused types and clear responsibilities over minimizing file or type count.
Code must be maintainable, testable, extensible, and readable.
Use explicit, meaningful symbol names. No abbreviations like msg, mesg, rec, dto, f, or tmp unless required by external SDK type names.

DESIGN CONSTRAINTS

1. The application model must be independent from Garmin SDK runtime objects after decode. Do not store Dynastream.Fit.Field or other SDK objects in the long-lived model.

2. Preserve original/raw metadata and values as immutable:
   - source field number / source field identifier
   - original FIT field name
   - original units
   - original raw value(s)
   - original decoded value(s)
   - original node/message type information
   - original developer/unknown classification

3. Allow editable working/presentation state separately:
   - display name
   - export inclusion flag
   - custom column name
   - edited decoded value(s)
   - other presentation-specific configuration

4. Unknown and developer-defined fields must be preserved, not dropped by default.
   They must also be easily filterable without deep object inspection.
   Prefer an explicit classification such as:
   - enum FitFieldKind { Standard, Developer, Unknown }
   or equivalent bool/enum-based metadata such as IsUnknown / IsDeveloperDefined

5. Array/multi-value FIT fields must be represented as actual collections, not loosely implied repeated fields.
   Prefer strongly expressed relations through explicit list/array/read-only collection properties rather than conventions encoded only in comments.
   Preserve array shape in the model.

6. The model must support later CSV export where each selected tree level can map to its own CSV:
   - activity.csv
   - session.csv
   - lap.csv
   - record.csv

7. The model must support later “reset edited values back to original decoded values”.

8. Keep unit conversion OUT of the immutable decoded data model.
   Unit selection belongs later in export/formatting/application service logic.
   The model should preserve source values in canonical/source units.
   Later export should support user-selectable units such as:
   - metric / imperial
   - time scaling
   - distance scaling
   - energy scaling such as J / kJ / kcal where appropriate
   Support an Auto mode concept for scalable quantities, where formatting/export chooses the most readable unit later.

9. Use immutable collections where they improve safety and intent.

10. Proper access modifiers matter.
    This solution lives in a larger MVVM architecture with separate model/viewmodel/view assemblies.
    Design with assembly boundaries in mind.
    If internal members are proposed, explain whether that assumes friend-assembly access or purely model-assembly-local usage.

IMPORTANT EXPORT-METADATA REQUIREMENT

The user will later configure export behavior by:
- selecting which node types to export,
- selecting which fields/columns to export,
- overriding column names

Therefore each node type or exportable field surface must support field/column naming metadata.

Important refinement:
- Do NOT rely only on CLR property names such as nameof(HeartRate)
- Many exportable fields may be dynamic FIT fields rather than fixed CLR properties
- Introduce a stable field key / export column key abstraction that works for both fixed modeled properties and dynamic FIT-derived fields

The initial export-column naming state should default to the original/source field name from the FIT file where applicable.

DELIVERABLES

Provide all of the following:

1. Architecture summary
   - short explanation of the recommended layers and boundaries
   - explain why Activity -> Session -> Lap and Record is the correct hierarchy
   - explain why GPS/location belongs as record-level data rather than a separate aggregate root

2. Recommended domain model
   - classes/records/enums/interfaces
   - clear ownership/aggregation relationships
   - explanation of why each type exists

3. Recommended field model
   - separate immutable field snapshot/definition from mutable working/export state
   - explain how scalar values vs array values are represented
   - explain how original/raw values and edited values coexist
   - explain how unknown/developer-defined fields are represented and filtered

4. Recommended caching strategy
   - assess whether caching should be included now
   - compare:
     a) no cache
     b) path-based cache
     c) path + length + last-write heuristic
     d) content-hash cache
   - recommend the best approach for a desktop app where a user may re-import the same file multiple times in one session
   - actual equality must be based on file content, not path/name/length alone
   - heuristics such as path + size + timestamp may be used only as prechecks, not proof of equality

5. Decoder implementation
   - prefer an instance-based decoder abstraction, not a static god class
   - define an interface such as IFitActivityDecoder
   - provide a concrete Garmin SDK-based implementation such as GarminFitActivityDecoder
   - validate input
   - decode a FIT file into the domain model
   - preserve unknown/developer fields
   - do not expose Garmin SDK types from the public API
   - use readable variable and method names

6. API-shape recommendation
   - recommend method signatures
   - recommend whether to accept string file path, Stream, or both
   - recommend whether to return DecoderResult<FitActivity> or a richer result type

7. Full code
   - provide the full recommended domain model code
   - provide the full decoder interface + Garmin implementation
   - provide helper types needed for field snapshots / field state / cache keys / results
   - split responsibilities into appropriate files/classes rather than one large file

8. Design assumptions / limitations
   - explicitly call out any places where Garmin SDK docs are incomplete and generated SDK/profile metadata are being used as the practical source of truth
   - call out assumptions about access modifiers across assemblies

PREFERRED MODEL CHARACTERISTICS

The design should clearly distinguish between:
- identity / metadata
- immutable raw/original data
- immutable original decoded data
- mutable working/edited data
- presentation/export configuration

A good solution will likely separate:
- immutable domain content
- mutable export/edit configuration

Possible acceptable patterns include:
- immutable entity + separate mutable configuration object
- immutable field snapshot + mutable field state wrapper
- immutable raw/original value + mutable edited decoded value

Pick the approach that best supports:
- later reset to original values
- later undo/redo
- later MVVM wrappers
- CSV export without redesigning the decoder model

RAW DATA ENCAPSULATION

Consider encapsulating immutable original/raw data explicitly.
For example, each major node may expose immutable source/original data through a dedicated raw/original snapshot object.
This should make the type surface more explicit in structure and intent.

If proposing internal raw/original members, explain clearly whether they stay model-assembly-local or require friend-assembly access.
Do not assume internal is visible to another assembly unless explicitly accounted for.

CURRENT CODEBASE CONTEXT

There are already rough placeholder classes such as Activity, Session, FitSessionFieldInfo, unit enums, and DecoderResult<T>, but these are not final and should not constrain the redesign.
You are allowed to replace them completely if a clearer design is better.

IMPORTANT STEP BOUNDARY

This work should be optimized for a two-step workflow:

Step A:
- domain model
- decoding boundary
- Garmin implementation
- optional cache design
- export metadata support in the model

Step B:
- exporter implementation

Design Step A so that Step B ideally does NOT require modifying the decoder model or decoding logic.
That ideal may not be perfectly achievable, but it is the target.

WHAT TO OPTIMIZE FOR

Optimize for:
- correctness
- clarity
- explicitness
- strong domain boundaries
- future editing/reset support
- future export support
- testability
- maintainability
- extensibility

Do NOT optimize for:
- minimum number of types
- shortest code
- cleverness
- tight coupling to the Garmin SDK

TEST PROJECT / VERIFICATION

This repository contains an xUnit test project named `FitToCsvConverter.Test`.

For the planning pass:
- inspect the existing test project structure and propose where tests for the decoder/model layer should live,
- identify which new tests should be added for the code produced in this step,
- do not treat tests as optional; testability is a required design goal.

For the implementation pass:
- place unit tests into the `FitToCsvConverter.Test` project,
- add or update focused xUnit tests for the decoding boundary and domain model behavior where appropriate,
- prefer deterministic unit tests over broad integration-style tests unless integration coverage is clearly justified.

At minimum, consider tests for:
- FIT activity hierarchy mapping (`Activity -> Session -> Lap / Record`)
- preservation of immutable original/raw data
- separation of immutable original data from editable working/export state
- unknown/developer field classification and filtering metadata
- array/multi-value field preservation
- cache-key behavior if caching is implemented
- reset of edited values back to original decoded values, if that behavior is included in this step

