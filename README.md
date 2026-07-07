# Volumetric Data Registration

A desktop application for **rigid registration of 3D volumetric data** (CT/MRI-style
scalar volumes). Given two volumes of the same object captured at different scales,
orientations, positions, and resolutions — typically a small, high-resolution
**micro** volume and a larger, lower-resolution **macro** volume — the program
estimates the 3D rigid transformation (rotation + translation) that aligns the micro
volume into the coordinate frame of the macro volume.

The pipeline is feature-based rather than intensity-based: instead of directly
optimising an image-similarity metric over the transformation parameters, it detects
salient points in each volume, describes their local neighbourhoods with rotation-invariant
feature vectors, matches those descriptors across the two volumes, turns each match into a
candidate transformation, and finally votes for the transformation supported by the largest
consistent cluster of candidates. This makes the method robust to large initial
misalignment, differing resolutions, and partial overlap, and it does not require an
initial pose estimate.

The application is built with [Avalonia UI](https://avaloniaui.net/) (.NET 9) and runs
cross-platform on Windows, macOS, and Linux. It provides a GUI for loading data,
slicing/inspecting volumes, running registration, and visually reviewing the alignment.

---

## Table of Contents

- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Downloading](#downloading)
  - [Building & Running](#building--running)
- [Input Data Format](#input-data-format)
- [Algorithm Overview](#algorithm-overview)
- [Project Structure](#project-structure)
- [Module Reference](#module-reference)
  - [Data Classes](#data-classes)
  - [Samplers](#samplers)
  - [Feature Computers](#feature-computers)
  - [Approximation](#approximation)
  - [Feature Normalization](#feature-normalization)
  - [Matching](#matching)
  - [Rotation / Transformation Computers](#rotation--transformation-computers)
  - [Density Estimation](#density-estimation)
  - [Transformation Refinement](#transformation-refinement)
  - [Transformation Distance Metrics](#transformation-distance-metrics)
  - [Registration Launcher](#registration-launcher)
  - [User Interface](#user-interface)
- [Configuration Constants](#configuration-constants)

---

## Getting Started

### Prerequisites

- [.NET SDK 9.0](https://dotnet.microsoft.com/download) or newer.
- Any OS supported by Avalonia (Windows, macOS, Linux).
- The **Avalonia UI** framework — the GUI will not compile without it (see below).
- A code editor is optional; [JetBrains Rider](https://www.jetbrains.com/rider/),
  [Visual Studio](https://visualstudio.microsoft.com/), or
  [VS Code](https://code.visualstudio.com/) with the C# extension all work well.

### Downloading

Clone the repository:

```bash
git clone <repository-url>
cd Registration
```

### Installing Avalonia

Compilation depends on the **Avalonia** NuGet packages (`Avalonia`, `Avalonia.Desktop`,
`Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`, `Avalonia.Svg.Skia`), together with the
other third-party dependencies (MathNet.Numerics, LiveCharts, Newtonsoft.Json). These are
already declared as `PackageReference`s in `Registration.csproj`, so in most cases you do
**not** need to install anything by hand — running `dotnet restore` (or simply building)
downloads them automatically from NuGet:

```bash
dotnet restore
```

If you are starting a project from scratch or the packages are missing, you can add
Avalonia explicitly with the .NET CLI:

```bash
dotnet add package Avalonia
dotnet add package Avalonia.Desktop
dotnet add package Avalonia.Themes.Fluent
dotnet add package Avalonia.Fonts.Inter
dotnet add package Avalonia.Svg.Skia
```

Optionally, installing the Avalonia project templates gives you `dotnet new avalonia.*`
scaffolding for future projects:

```bash
dotnet new install Avalonia.Templates
```

### Building & Running

From the project root:

```bash
# Restore NuGet packages (also done implicitly by build/run)
dotnet restore

# Build
dotnet build

# Launch the GUI application
dotnet run
```

For an optimized build, use `dotnet run -c Release`. In an IDE you can simply open the
folder / `Registration.csproj` and press *Run*.

## Input Data Format

Volumes are provided as a pair of files in the common **MetaImage** convention:

- **`.mhd`** — an ASCII metadata/header file describing dimensions (`DimSize`), voxel
  spacing (`ElementSpacing`), data type, etc.
- **`.raw`** — the raw binary voxel payload (read as 16-bit unsigned integers).

Each volume is loaded through a [`FilePathDescriptor`](ApplicationCode/Other/FilePathDescriptor.cs)
that pairs the header and data paths.

---

## Algorithm Overview

The end-to-end registration is orchestrated by
[`RegistrationLauncher.RunRegistration`](ApplicationCode/RegistrationLaunchers/RegistrationLauncher.cs).
The stages are:

1. **Sampling.** Salient candidate points are drawn from each volume by a **Sampler**.
   Rather than sampling uniformly, samplers favour regions with high local variance /
   structure, since flat/background regions carry no discriminative information.

2. **Feature extraction.** For every sampled point, a **Feature Computer** builds a
   **feature vector** — a compact, rotation-invariant numeric description of the local
   neighbourhood (curvature, gradient magnitude, shape index, intensity quantiles, PCA
   eigenvalue profile, …). Several computers are combined into one **CompoundFeatureComputer**.
   Feature computation is multi-threaded across all CPU cores, and vectors containing
   `NaN`/`Inf` values (e.g. from perfectly flat neighbourhoods) are discarded.

3. **Normalization & weighting.** Feature values are mapped onto a common scale by a
   **Feature Normalizer** (rank / empirical-CDF based, so it is robust to outliers and
   aligns the micro and macro feature distributions), then multiplied by per-feature
   weights so more reliable features dominate the distance metric.

4. **Matching.** A **Matcher** finds, for each micro feature vector, its most similar
   macro feature vector using a KD-tree nearest-neighbour search, producing a set of
   putative point correspondences (**matches**). Unstable correspondences are filtered
   (via Lowe's ratio test or a spatial-variance heuristic depending on configuration).

5. **Transformation hypotheses.** For each match, a **Transformer** estimates a local
   coordinate basis at both points (via PCA of the neighbourhood) and derives the rigid
   rotation aligning them, plus the corresponding translation — giving one (or a few)
   candidate `Transform3D` per match.

6. **Density voting.** All candidate transformations are clustered in transformation
   space by a **DensityStructure**. The transformation lying in the densest cluster — i.e.
   the one that the largest number of independent matches agree on — is selected as the
   result.

7. **(Optional) Refinement.** The single-match winner can be refined with a least-squares
   rigid fit (Kabsch) over all consistent matches (**TransformationRefiner**) to average
   out per-correspondence noise.

Progress and cancellation across these stages flow through a
[`RegistrationProgress`](ApplicationCode/RegistrationLaunchers/RegistrationProgress.cs)
object so the GUI can display a live progress bar and abort a run.

---

## Project Structure

```
Registration/
├── Program.cs                     # Entry point (GUI launch + headless test switch)
├── App.axaml(.cs) / MainWindow    # Avalonia application shell & navigation
├── Registration.csproj            # .NET 9 project + NuGet dependencies
├── Views/                         # Avalonia UI (XAML + code-behind)
└── ApplicationCode/
    ├── DataClasses/               # Volume representation, slicers, mock objects
    ├── Samplers/                  # Point selection strategies
    ├── FeatureComputers/          # Local descriptors (feature vectors)
    ├── Approximation/             # Local quadric fitting shared by feature computers
    ├── FeatureNormalization/      # Feature scaling strategies
    ├── Matching/                  # KD-tree + correspondence matchers
    ├── RotationComputers/         # Match → rigid transformation estimation
    ├── Density/                   # Transformation clustering / voting
    ├── TransformationDistanceMetrics/  # Distance metrics in transformation space
    ├── RegistrationLaunchers/     # Pipeline orchestration
    ├── Other/                     # Core types (Point3D, Transform3D, FeatureVector, …)
    └── Test/                      # Headless tests, benchmarks, mock builders
```

---

## Module Reference

### Data Classes

`ApplicationCode/DataClasses/`

- **`AData` / `AMockObject`** — abstract base defining the volume interface: value lookup
  at a (possibly non-integer, interpolated) 3D coordinate, bounds, voxel spacings,
  min/max values, percentiles, and normalized value access.
- **`VolumetricData`** — the concrete volume backed by `.mhd`/`.raw` files. Stores the
  voxel grid as `ushort[][,]`, reads element spacing from metadata, and supports
  interpolated sampling at arbitrary continuous coordinates.
- **`VolumetricDataDistribution`** — tracks the intensity histogram/distribution of a
  volume (used for percentile and normalization queries).
- **`Slicers/`** (`DataSlicer`, `TransformedDataSlicer`, `DataSamplerSlicer`,
  `DataFCSlicer`) — extract 2D cross-sectional slices from a volume for visualisation,
  including views of the transformed volume, of the sampled points, and of feature
  values, used by the GUI's slicer/alignment tools.
- **`MockObjects/`** (`SphereMockData`, `EllipsoidMockData`, `SpheresMockData`,
  `PointDistanceMock`, `SmallObjectMock`, `MockDataSegment`) — synthetic, analytically
  defined volumes used for testing and debugging without real scan data.

### Samplers

`ApplicationCode/Samplers/` — decide *where* in a volume to place candidate points.
All implement `ISampler.Sample(AData, count)`.

- **`Sampler`** — baseline uniform random sampler.
- **`SamplerVariance`** — accepts random points only when the local intensity variance
  exceeds a threshold (skips flat regions).
- **`SamplerPercentile`** — over-samples random candidates, scores each by the variance
  of its 3×3×3 neighbourhood, and keeps the top percentile (highest-variance points).
  Points may repeat spatially; pairs well with `PositionMatcher`.
- **`SamplerGradient`** — keeps points whose local gradient magnitude exceeds a minimum,
  targeting edges/boundaries.
- **`SamplerSegmented`** — divides the volume into a uniform grid of cells; within each
  cell only the highest-variance candidate survives, and of those only the best fraction
  is returned. Guarantees every returned point covers a **distinct** region (no repeated
  sampling of the same spot), which is the property Lowe's ratio test in `MatcherLowe`
  relies on. Cells below a minimum relative standard deviation are treated as background
  and produce no sample.
- **`UniformSphereSampler`** — not a point *selector* but a shared utility that
  precomputes a set of points uniformly distributed on/within a sphere. Feature computers
  and the PCA basis estimator call `GetDistributedPoints` to gather a rotation-invariant,
  isotropic neighbourhood around a query point.
- **`SamplerFake`** — deterministic stub for tests.

### Feature Computers

`ApplicationCode/FeatureComputers/` — build the per-point feature vector.
All derive from `AFeatureComputer`, which exposes `NumberOfFeatures`, `ComputeFeatureVector`,
per-feature `GetWeights`, and a serialisable `GetConfiguration`.

- **`CompoundFeatureComputer`** — composes several feature computers into one, laying
  their outputs out contiguously in a single feature vector and concatenating their
  weights. This is what the pipeline actually uses.
- **`FeatureComputerPointValue`** — the raw (interpolated) intensity at the point. Single
  feature; simple but not rotation-invariant on its own.
- **`FeatureComputerQuantiles`** — samples values over the spherical neighbourhood and
  emits a configurable set of intensity quantiles (e.g. the 5/25/50/75/95th percentiles).
  Rotation-invariant summary of the local intensity distribution.
- **`FeatureComputerGradient`** (and `FeatureComputerGradient2`) — the gradient magnitude
  of the locally fitted quadric surface. Uses a shared `AApproximationComputer`; the `2`
  variant differs in the sampling-weight (elliptical vs. linear) strategy.
- **`FeatureComputerISOSurfaceCurvature`** (`FeatureComputerISOCurvature`, and the `2`
  variant) — Gaussian and mean curvature of the ISO-surface passing through the point,
  derived from the fitted quadric. Two features.
- **`FeatureComputerShapeIndex`** — the Koenderink **shape index** and **curvedness** of
  the local ISO-surface, derived from the principal curvatures. The shape index ∈ [-1, 1]
  describes local surface type (cup / rut / saddle / ridge / cap) independently of scale,
  and curvedness captures its magnitude. Better-behaved under normalization than raw
  Gaussian/mean curvature (which have heavy tails).
- **`FeatureComputerPCALength`** — performs PCA on the (value-weighted) neighbourhood
  point cloud and emits the normalized eigenvalues, describing the local anisotropy /
  shape elongation. Three features.
- **`FeatureComputerSphericalHarmonics`** — expands the neighbourhood onto spherical
  harmonics and returns the rotation-invariant power spectrum per band (up to `lMax`).
- **`FakeFeatureComputer`** — testing stub that can inject a known expected transformation.

### Approximation

`ApplicationCode/Approximation/`

Curvature, gradient, and shape-index features all need the same thing: a **local quadric
(second-order polynomial) approximation** of the volume around a point, obtained by a
weighted least-squares fit over the spherical neighbourhood.

- **`AApproximationComputer`** — computes that fit once and, crucially, **caches it
  per-thread** so multiple feature computers evaluating the same point reuse a single
  expensive computation instead of recomputing it. It accumulates the normal equations
  `(AᵀWA)x = AᵀWv` directly (avoiding materialising huge sample matrices) and restricts
  sampling to the sphere of `PROXIMITY_RADIUS`.
- **`ApproximationComputerLinear`** / **`ApproximationComputerElliptical`** — concrete
  strategies differing only in the per-sample weighting function (linear vs. elliptical
  interpolation weighting).
- **`ApproximationResult`** — holds the fitted quadric coefficients and the grid-aligned
  centre point.

### Feature Normalization

`ApplicationCode/FeatureNormalization/`

- **`FeatureNormalizer`** — min/max (range) based scaling of features across the sampled
  sets.
- **`FeatureNormalizerRank`** — **rank / empirical-CDF** normalization: each feature is
  mapped to its rank fraction within its set, so every normalized feature lies in [0, 1]
  regardless of the original distribution. Insensitive to outliers and heavy tails, and
  aligns the micro and macro distributions even when their raw value ranges differ. This
  is the normalizer used by the main pipeline.

### Matching

`ApplicationCode/Matching/` — establish point correspondences between the two feature sets.
All implement `IMatcher.Match(microFVs, macroFVs, threshold)`.

- **`KDTree`** — a k-d tree over feature vectors enabling fast nearest-neighbour queries
  in feature space (the backbone of every real matcher).
- **`Match`** — a micro↔macro feature-vector pair plus a similarity score; orders by
  similarity so the best matches can be kept.
- **`Matcher`** — nearest-neighbour matcher scoring matches by cosine similarity and
  keeping the top fraction.
- **`MatcherLowe`** — nearest-neighbour matcher with **Lowe's ratio test** (as in SIFT):
  a match is kept only when the closest candidate is clearly more similar than the
  runner-up (`d₁/d₂ ≤ ratioThreshold`), rejecting ambiguous correspondences. Requires a
  sampler that visits each salient region only once (e.g. `SamplerSegmented`).
- **`PositionMatcher`** — collects all sufficiently similar macro candidates per micro
  point and scores the correspondence by the **spatial variance** of those candidates:
  when a descriptor's good matches cluster tightly in space the correspondence is trusted.
  Designed to pair with repeated-sampling samplers such as `SamplerPercentile`.
- **`NaiveMatcher`** / **`RelativeMatcher`** — simpler / experimental matching strategies.
- **`FakeMatcher`** — testing stub.

> The launcher currently defaults to **`SamplerPercentile` + `PositionMatcher`**
> (Option B); an alternative **`SamplerSegmented` + `MatcherLowe`** configuration
> (Option A) is provided in the constructor and can be swapped in.

### Rotation / Transformation Computers

`ApplicationCode/RotationComputers/`

- **`ATransformer`** — abstract base turning a single `Match` into candidate `Transform3D`s.
  It obtains rotation matrix/matrices from the subclass, then computes the matching
  translation vector (so that the rotated micro point lands on the macro point).
- **`UniformRotationComputerPCA`** (`UniformRotationComputerPCA2.cs`) — estimates a local
  orthonormal **basis** at each point via PCA of its spherical neighbourhood (with a
  gradient-based sign disambiguation), then computes the rotation that maps the micro
  basis onto the macro basis. Because PCA leaves a sign/axis ambiguity, it returns a small
  set of candidate rotations per match, all of which are voted on downstream.

### Density Estimation

`ApplicationCode/Density/`

- **`DensityStructure`** — the voting stage. Given all candidate transformations, it
  scores each by a Gaussian-weighted **density** of neighbouring transformations (sum of
  `exp(-spread·dist²)`) and returns the transformation in the densest region. The search
  radius and spread parameter are derived from a threshold and the median inter-transformation
  distance.
- **`DensityTree`** — spatial index over transformations supporting the
  radius/proximity queries the density computation needs.
- **`DensityNaive`** — a brute-force reference implementation of the same idea.

Distances between transformations are measured with a pluggable
[transformation distance metric](#transformation-distance-metrics).

### Transformation Refinement

`ApplicationCode/Other/TransformationRefiner.cs`

Refines the density-voting winner (which comes from a *single* match and is therefore
limited by that one correspondence's noise) with a least-squares rigid fit
(**Kabsch algorithm**) over all matches consistent with it. It runs several rounds with a
shrinking inlier tolerance so later rounds are driven only by accurate correspondences.
(Currently wired up but commented out in the default launcher path.)

### Transformation Distance Metrics

`ApplicationCode/TransformationDistanceMetrics/`

Define how "far apart" two rigid transformations are — needed by the density clustering.
All implement `ITransformationDistance` (`GetTransformationsDistance` and a relative
variant). Several strategies (`NaiveTransformationDistance`, `TransformationDistance1`…`7`)
explore different ways of combining rotational and translational error, e.g. by measuring
how far a set of representative points is displaced when both transformations are applied.
`TransformationDistanceSeven` (parameterised by the micro volume) is the metric selected in
the launcher.

### Registration Launcher

`ApplicationCode/RegistrationLaunchers/`

- **`IRegistrationLauncher`** / **`RegistrationLauncher`** — assembles the concrete
  sampler, feature computer, matcher, and transformer, then drives the full pipeline in
  `RunRegistration(...)`. Also hosts diagnostic routines (`FeatureVarianceTest`,
  `FeatureDifferenceMean`, `FeatureVarianceInDifferentRegions`, `FeatureVectorTest`) used
  to evaluate feature discriminativeness and export CSV analyses.
- **`RegistrationProgress`** — carries stage/percentage callbacks and a
  `CancellationToken` so the UI can report progress and cancel a run.
- **`FakeRegistrationLauncher`** — stub for UI testing.

### User Interface

`Views/` — Avalonia XAML views with C# code-behind. Navigation is driven from
`MainWindow`, which swaps the main content between:

- **`RegistrationView`** — load micro/macro `.mhd`/`.raw` files and run the registration,
  with a live multi-stage progress bar and cancellation.
- **`SlicerSetupView` / `SlicerView`** — load and browse a volume slice-by-slice.
- **`AlignmentView`** — visually inspect how well the micro volume overlays the macro
  volume under the computed transformation.
- **`ExperimentSetupDialog` / `ExperimentView`** — configure and run parameterised
  experiments over feature-computer configurations.
- **`Controls/`** (`ButtonView`, `IndicatorView`) — small reusable UI components (e.g.
  file-loaded status indicators).

---

## Configuration Constants

Global tunables live in [`ApplicationCode/Other/Constants.cs`](ApplicationCode/Other/Constants.cs):

| Constant | Default | Meaning |
| --- | --- | --- |
| `NUMBER_OF_POINTS_MICRO` | 40 000 | Points sampled from the micro volume |
| `NUMBER_OF_POINTS_MACRO` | 40 000 | Points sampled from the macro volume |
| `THRESHOLD` | 0.1 | Fraction of best matches kept |
| `RADIUS` / `SPACING` | 3 / 0.225 | Uniform-sphere neighbourhood radius & spacing |
| `PROXIMITY_RADIUS` | 18 | Radius of the neighbourhood used for quadric fitting |
| `PROXIMITY_SPACING` | 0.5 | Minimum grid spacing used while sampling neighbourhoods |

---

*This documentation reflects the feature-based rigid registration pipeline as implemented
in `RegistrationLauncher`. The codebase also contains a number of experimental variants
(alternative matchers, samplers, distance metrics, and feature computers) that can be
swapped in via the launcher's constructor to explore different registration strategies.*
