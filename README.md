
# 1. Monoceros
![Monoceros](./readme-assets/monoceros32.png) **Monoceros**: a Wave Function Collapse plug-in for Grasshopper by Subdigital

## 1.1. Authors
- Ján Pernecký: [jan@sub.digital](mailto:jan@sub.digital)
- Ján Tóth: [GitHub](https://yanchith.github.io)
- **Subdigital**: [sub.digital](https://www.sub.digtial), [GitHub](https://github.com/subdgtl)

## 1.2. Tl;dr
**Monoceros is a Grasshopper plug-in that fills the entire world with Modules, respecting the given Rules.**

## 1.3. Table of contents
- [1. Monoceros](#1-monoceros)
  - [1.1. Authors](#11-authors)
  - [1.2. Tl;dr](#12-tldr)
  - [1.3. Table of contents](#13-table-of-contents)
  - [1.4. Meet Monoceros](#14-meet-monoceros)
  - [1.5. Development notes](#15-development-notes)
  - [1.6. Architecture of Monoceros Grasshopper plug-in](#16-architecture-of-monoceros-grasshopper-plug-in)
  - [1.7. Data types](#17-data-types)
    - [1.7.1. Slot](#171-slot)
      - [1.7.1.1. States](#1711-states)
      - [1.7.1.2. Slot Properties](#1712-slot-properties)
      - [1.7.1.3. Automatic Envelope wrapping](#1713-automatic-envelope-wrapping)
      - [1.7.1.4. Modules and their Parts](#1714-modules-and-their-parts)
      - [1.7.1.5. Viewport preview and baking](#1715-viewport-preview-and-baking)
      - [1.7.1.6. Slot casts to](#1716-slot-casts-to)
    - [1.7.2. Module](#172-module)
      - [1.7.2.1. Monoceros Module Parts](#1721-monoceros-module-parts)
      - [1.7.2.2. Connectors](#1722-connectors)
      - [1.7.2.3. Module Geometry](#1723-module-geometry)
      - [1.7.2.4. Orientation and placement](#1724-orientation-and-placement)
      - [1.7.2.5. Module Properties](#1725-module-properties)
      - [1.7.2.6. Special Modules: Out and Empty](#1726-special-modules-out-and-empty)
      - [1.7.2.7. Viewport preview and baking](#1727-viewport-preview-and-baking)
      - [1.7.2.8. Module casts](#1728-module-casts)
    - [1.7.3. Rule](#173-rule)
      - [1.7.3.1. Explicit Rule](#1731-explicit-rule)
        - [1.7.3.1.1. Explicit Rule properties](#17311-explicit-rule-properties)
        - [1.7.3.1.2. Explicit Rule casts](#17312-explicit-rule-casts)
        - [1.7.3.1.3. Explicit Rule Viewport preview and baking](#17313-explicit-rule-viewport-preview-and-baking)
      - [1.7.3.2. Typed Rule](#1732-typed-rule)
        - [1.7.3.2.1. Typed Rule properties](#17321-typed-rule-properties)
        - [1.7.3.2.2. Typed Rule casts](#17322-typed-rule-casts)
        - [1.7.3.2.3. Typed Rule Viewport preview and baking](#17323-typed-rule-viewport-preview-and-baking)
      - [1.7.3.3. Indifferent Typed Rule](#1733-indifferent-typed-rule)
  - [1.8. Components](#18-components)
    - [1.8.1. Slot-related](#181-slot-related)
      - [1.8.1.1. Construct Slot With All Modules Allowed](#1811-construct-slot-with-all-modules-allowed)
      - [1.8.1.2. Construct Slot With Listed Modules Allowed](#1812-construct-slot-with-listed-modules-allowed)
      - [1.8.1.3. Deconstruct Slot](#1813-deconstruct-slot)
      - [1.8.1.4. Are Slots Boundary](#1814-are-slots-boundary)
      - [1.8.1.5. Add Boundary Layer](#1815-add-boundary-layer)
    - [1.8.2. Module-related](#182-module-related)
      - [1.8.2.1. Construct Module](#1821-construct-module)
      - [1.8.2.2. Construct Empty Module](#1822-construct-empty-module)
      - [1.8.2.3. Deconstruct Module](#1823-deconstruct-module)
    - [1.8.3. Rule-related](#183-rule-related)
      - [1.8.3.1. Construct Explicit Rule](#1831-construct-explicit-rule)
      - [1.8.3.2. Deconstruct Explicit Rule](#1832-deconstruct-explicit-rule)
      - [1.8.3.3. Is Rule Explicit](#1833-is-rule-explicit)
      - [1.8.3.4. Construct Typed Rule](#1834-construct-typed-rule)
      - [1.8.3.5. Deconstruct Typed Rule](#1835-deconstruct-typed-rule)
      - [1.8.3.6. Is Rule Typed](#1836-is-rule-typed)
      - [1.8.3.7. Unwrap Typed Rules](#1837-unwrap-typed-rules)
      - [1.8.3.8. Collect Rules](#1838-collect-rules)
      - [1.8.3.9. Explicit Rule From Curve](#1839-explicit-rule-from-curve)
      - [1.8.3.10. Typed Rule From Point](#18310-typed-rule-from-point)
      - [1.8.3.11. Rule At Boundary From Point](#18311-rule-at-boundary-from-point)
      - [1.8.3.12. Indifferent Rule From Point](#18312-indifferent-rule-from-point)
      - [1.8.3.13. Indifferent Rules For Unused Connectors](#18313-indifferent-rules-for-unused-connectors)
    - [1.8.4. Solver](#184-solver)
      - [1.8.4.1. Monoceros WFC Solver](#1841-monoceros-wfc-solver)
    - [1.8.5. Post processing](#185-post-processing)
      - [1.8.5.1. Materialize Slots](#1851-materialize-slots)
      - [1.8.5.2. Assemble Rule](#1852-assemble-rule)
    - [1.8.6. Supplemental](#186-supplemental)
      - [1.8.6.1. Slice Geometry](#1861-slice-geometry)
  - [1.9. Examples](#19-examples)
    - [1.9.1. Bare minimum](#191-bare-minimum)
      - [1.9.1.1. Pseudo code](#1911-pseudo-code)
      - [1.9.1.2. Definition](#1912-definition)
      - [1.9.1.3. Breakdown](#1913-breakdown)
    - [1.9.2. Defining more Modules and Explicit Rules](#192-defining-more-modules-and-explicit-rules)
      - [1.9.2.1. Pseudo code (almost) without data trees](#1921-pseudo-code-almost-without-data-trees)
      - [1.9.2.2. Definition (almost) without data trees](#1922-definition-almost-without-data-trees)
      - [1.9.2.3. Pseudo code with data trees](#1923-pseudo-code-with-data-trees)
      - [1.9.2.4. Definition with data trees](#1924-definition-with-data-trees)
      - [1.9.2.5. Result](#1925-result)
    - [1.9.3. Indifferent Rules](#193-indifferent-rules)
    - [1.9.4. Typed Rules](#194-typed-rules)
    - [1.9.5. Disallowing Rules](#195-disallowing-rules)
    - [1.9.6. Modules with more Parts](#196-modules-with-more-parts)
    - [1.9.7. Module points from Module geometry](#197-module-points-from-module-geometry)
    - [1.9.8. Empty Module](#198-empty-module)
    - [1.9.9. Allowing an Empty neighbor](#199-allowing-an-empty-neighbor)
    - [1.9.10. Choosing boundary Modules](#1910-choosing-boundary-modules)
    - [1.9.11. Slots from geometry](#1911-slots-from-geometry)
    - [1.9.12. Extreme Slot Envelopes](#1912-extreme-slot-envelopes)
    - [1.9.13. Allowing certain Modules in certain Slots](#1913-allowing-certain-modules-in-certain-slots)
    - [1.9.14. Disallowing certain Modules from certain Slots](#1914-disallowing-certain-modules-from-certain-slots)
    - [1.9.15. Setting fixed Modules](#1915-setting-fixed-modules)
    - [1.9.16. Materializing results](#1916-materializing-results)
    - [1.9.17. Proto-results and custom materialization](#1917-proto-results-and-custom-materialization)
    - [1.9.18. What makes a good Module](#1918-what-makes-a-good-module)
    - [1.9.19. Random seed and attempts count](#1919-random-seed-and-attempts-count)
    - [1.9.20. Making a valid Envelope](#1920-making-a-valid-envelope)
  - [1.10. MIT License](#110-mit-license)

## 1.4. Meet Monoceros
![Monoceros](readme-assets/monoceros512.png)

>Monoceros is a legendary animal living in the huge mountains in the interior of India. Monoceros has the body of a horse, the head of a stag, the feet of an elephant and the tail of a boar. [from Unicorn Wiki](https://karkadann.fandom.com/wiki/Monoceros)


It is also a plug-in for [Grasshopper](https://www.grasshopper3d.com), which is a visual programming platform for [Rhinoceros](https://www.rhino3d.com) 3D CAD software. Monoceros was developed at studio [Subdigital](https://www.sub.digital) by Ján Toth and Ján Pernecký. Monoceros is an implementation of the Wave Function Collapse (WFC) algorithm developed for game design by [Maxim Gumin](https://github.com/mxgmn/WaveFunctionCollapse) and extended and promoted by [Oskar Stålberg](oskarstalberg.com/) with his game [Townscaper](https://store.steampowered.com/app/1291340/Townscaper/).

![grasshopper panel](readme-assets/grasshopper-panel.png)

Monoceros serves to fill the entire world with Modules, respecting the given Rules. The plug-in wraps WFC into a layer of abstraction, which makes WFC easily implemented in architectural or industrial design. It honors the principles of WFC and Grasshopper at the same time - offering a full control of the input and output data in a Grasshopper way and their processing with a pure WFC.

## 1.5. Development notes
This repository contains the Grasshopper wrapper for the main WFC solver and comprehensive supplemental tools.

*The solver itself was developed in Rust and compiled as a `.dll` library linked to this wrapper. The source code of the solver and a simple wrapper component for Grasshopper is in a separate [repository](https://github.com/subdgtl/WFC).*

The Monoceros Grasshopper plug-in is written in C## and revolves around three main data types:

1. **Slot** is the basic cuboid unit of a discrete world. The Slots can be embedded with Modules or their Parts. Initially the Slots allow containment of multiple Modules until the WFC solver reduces the list of allowed Modules to a single Module for each Slot according to given Rules.
2. **Module** represents geometry wrapped into one or more cuboid cages (similar to the Slots). Modules are about to be placed into the Slots according the given Rules.
3. **Rule** describes allowed adjacency of two Modules or their Parts via one of the walls of the cuboid cages - connectors.

The Monoceros plug-in offers various Grasshopper **components** (functions) for constructing and parsing the data, the solver itself and postprocessing and rendering tools.

## 1.6. Architecture of Monoceros Grasshopper plug-in
The core of Monoceros is a Wave Function Collapse (WFC) solver. WFC is an algorithm, that fills the entire discrete envelope with Modules with no remaining empty Slot. In case of Monoceros, the envelope is a collection of rectangular cuboid Slots, each with 6 neighbors in orthogonal directions, not taking diagonal neighbors into account.  
In the original WFC algorithm, the Modules are exactly the size of a single Slot. The WFC then picks which Module should be placed into which Slot, leaving no Slot non-deterministic (with more than one Module allowed to be placed into the Slot) or empty / contradictory (no Module allowed to be placed into the Slot). Usually, there is less Modules (Module types) than Slots, which means each Module can be placed into Slots more times or not at all.  
The Monoceros implementation of WFC internally works like this too, on the outside it presents the Modules as a continuous coherent compact collection of such cuboid cages (Module Parts), each fitting into one Slot.

Like Grasshopper itself, also Monoceros revolves around data and serves for its immutable processing. Immutability means, that no existing data is being changed but rather transformed and returned as a new instance of the data. In most cases it is even possible to construct the data with valid values right away with no need to re-define already existing data.  
There are three main data types: **Slot**s, **Module**s and **Rule**s.

Slot and Rule both reference to Module, its Part or its Connector. This reference is done only through user defined strings (for Modules and their Parts) or integer indices (for Module Connectors). This is an intention, so that the data sets (Modules, Rules or Slots) can be replaced or shared across more Monoceros setups.

|   Construct |        |     Solve      |        | Post process    |
| ----------: | :----: | :------------: | :----: | :-------------- |
| **Modules** | **->** |                |        |                 |
|   **Rules** | **->** | **WFC Solver** | **->** | **Materialize** |
|   **Slots** | **->** |                |        |                 |
|             |        |   Aggregate    |        |                 |
|             |        |    Preview     |        |                 |

Most of the Monoceros plug-in components serve for constructing, analyzing and processing data. The components try not to bring redundancy, therefore it does not do anything, that could be easily done with vanilla Grasshopper components. The three new Monoceros data types are seamlessly integrated into Grasshopper and cast from and to all relevant existing data types. All Monoceros components are compatible with the existing Grasshopper data types and ready to be used with existing Grasshopper components.  

## 1.7. Data types

### 1.7.1. Slot
Slot is a cuboid (orthogonal box) that represents the basic unit of the Monoceros rigid discrete grid. The Slots do not overlap and their position coordinates are defined in discrete numerical steps. The Slots are stacked next to each other, preferably forming a coherent continuous blob or more separate blobs that should become filled with Modules. Such blob will be called an Envelope.

#### 1.7.1.1. States
Slot is a container that allows placement of certain Modules or their Parts. The Slot can be in several states:
1. **Allows Nothing** is a contradictory (invalid) state, when there is no Module or its Part that can be placed inside the Slot. If the collection of Slots forming the Envelope of the solution contains one or more such Slots, the solution cannot be found and therefore such setup is invalid.
2. **Allows one Module** (or deterministic state) is the desired state of a Slot. It is the responsibility of the WFC Solver to bring a non-deterministic Slot into a deterministic state. Such Slot can be Materialized - a Module can be placed into the Slot.
3. **Allows more Modules** (or non-deterministic state) is usually an initial or intermediate state of a Slot, when it is not yet clear, which Module or its Part should be placed inside the Slot, but a list of allowed Modules is present. It is the responsibility of the WFC Solver to bring a non-deterministic Slot into a deterministic state. A non-deterministic Slot cannot be Materialized yet but it is possible to evaluate its level of entropy - a number of currently allowed Modules or their Parts stating how far from the deterministic state the Slot is.
4. **Allows all Modules** is a shortcut for a fully non-deterministic state, when any Module or its Part is allowed to be placed inside the Slot. In practice this state exists only for cases, when Slots are being defined before or independently from Modules and therefore it is not possible to list Modules that should be allowed by the Slot.

The collection of Slots forming an Envelope is considered Canonical if the adjacent Slots allow placement of such Modules or their Parts, that are allowed to be neighbors by the Rules. Canonical Envelope does not have to be also deterministic. For non-deterministic Slots to form a Canonical Envelope, each allowed Module or its Part in one Slot must be allowed to be a neighbor of each allowed Module or its Part in its adjacent Slot in the given direction by the Rule set.

The Monoceros implementation of the WFC algorithm can automatically clean a Non-Canonical Envelope into Canonical, which takes a lot of responsibility from the user and enables future development of Monoceros features.

#### 1.7.1.2. Slot Properties
- **Center** of the Slot in cartesian coordinate system. The coordinate is automatically rounded so that it represents an exact center of the Slot in the discrete world coordinate system.
- **Base Plane** defining Slot's coordinate system. The discrete world coordinate system's origin and axial orientation matches that of the Base Plane. For two Slots to be compatible, their Base Planes must match.
- **Diagonal** defining Slot's dimensions in X, Y and Z directions as defined by the Base Plane. For two Slots to be compatible, their Diagonals must match.
- **Allowed Module Names** is a list of Modules that are allowed to be placed (entire Modules or their Parts) inside the Slot. This list is empty, when the Slot is in the Allows Nothing or Allows Everything state.
- **Allows Everything** is a flag determining whether the Slot allows placement of any Module or its Part.
- **Allows Nothing** is a flag determining whether the Slot allows placement of no Module or its Part. Such Slot is invalid and prevents the WFC Solver from reducing other Slots from non-deterministic to deterministic state.

#### 1.7.1.3. Automatic Envelope wrapping
The WFC algorithm (the original one and the Monoceros implementation) work with a full regular three-dimensional box-like Envelope. Monoceros allows the user to define any set of Slots (as long as they are compatible and non-repetitive), which form any arbitrary blob. 

Therefore the Monoceros WFC Solver component automatically wraps the user-defined Slots into a slightly larger bounding box and adds the missing Slots. These Slots are dubbed Out-enabled and are pre-determined to allow only one type of a Module to be placed inside them: the Out Module. Out Module is automatically generated by the Monoceros WFC Solver, it has the same diagonal as the Envelope Slots, has just a single Part and contains no geometry. The Out Module has all connectors defined as Indifferent, therefore it is allowed to be adjacent to itself and with any other Module Indifferent in the respective direction. The Out Module and an Empty Module are distinguished so that it is possible to set a Rule for a Module Connector to be adjacent to Out Module (while not being Indifferent), which allows the Module to be placed at the boundary of the Envelope. 

All boundary Slots are ensured to be surrounded by Out-enabled Slots. The Out-enabled Slots are not being displayed in the Rhinoceros viewport.

#### 1.7.1.4. Modules and their Parts
The Monoceros Modules may consist of more Parts. Each Part is of the size of a single Slot, therefore a larger Module occupies more Slots. For valid Modules it is ensured that the Module Parts always hold together and all of the Module Parts are always placed into Slots in the original order. Internally, the Slots refer to Module Parts, for the user of Monoceros Grasshopper plug-in are the Module Parts inaccessible and the Module appears as the smallest unbreakable unit. Therefore it is only possible to define entire Modules to be allowed by a Slot, even when such Module consists of more Parts. Internally this means, that all Parts of the Module are allowed to be placed into a Slot. In practice this should not cause any problems or imprecisions. It is being automatically handled by the WFC Solver and offers some buffer zone for complex Module placement.

#### 1.7.1.5. Viewport preview and baking
A Slot renders in the viewport as a wire frame box. The box is slightly smaller than the actual Slot, so that it is possible to distinguish colors of two adjacent Slots from one another. 

A Slot bakes as a regular Rhino box, with dimensions matching the Slot size.

A Slot renders and bakes in different colors, indicating the level of Slot's entropy:
- **Red** - the Slot does not allow placement of any Module or its Part, therefore is empty and invalid.
- **White** - the Slot allows placement of any Module or its Part that enters the Monoceros WFC Solver. 
- **Blue** - the Slot allows placement of more than one Module or its Part but the overall number of Modules is unknown to the Slot. This is a valid state for Slots defined by enumerating the allowed Modules. A Slot is blue even when all Modules are allowed, but they have been listed namely instead of using the Allow all constructor.
- **Green** - the Slot allows placement of exactly one Module Part and is ready to be Materialized. This is currently only possible for Slots processed by the Monoceros WFC Solver. Green color denotes the desired state of a Slot. 
- **Grey** - the Slot allows placement of some Modules or their Parts and the overall number of Modules and their Parts is known. Bright color indicates a high level of entropy - more Modules or their Parts are (still) allowed to be placed into the Slot. Dark grey means the Slot has a lower entropy and is closer to a solution.

#### 1.7.1.6. Slot casts to
The Casts are meant to shorten the de-construction and re-construction of a Slot. With the following casts it is possible to use one instance of a Slot to construct a new one with the same properties by passing the Slot into individual input Slots of a Slot constructor component.
- **Point** - representing the center point of the Slot
- **Box** and **BRep** - representing the exact box cage of the Slot
- **Vector** - representing the Diagonal of the Slot
- **Text** (string) - returns a human-friendly report of the Slot's properties in format: `Slot allows placement of XY modules. Slot dimensions are XYZ, center is at XYZ, base plane is XYZ X Y.`

### 1.7.2. Module
Module is a unit, which is being distributed over the specified Envelope. The main purpose of the WFC and Monoceros is to decide, which Module or its Part can be placed into which Slot, so that the adjacencies of Modules follows the specified Rule set. If this requirement is met, it means the Envelope is Canonical. If there is exactly one Module or its Part allowed to be placed into every Slot, the Envelope is Deterministic and solved.

#### 1.7.2.1. Monoceros Module Parts
In the original WFC algorithm, the Module occupies exactly one Slot. Monoceros offers a possibility for Modules to span over (occupy) more Slots. In such case, the Module consists of more Parts, each of a size of a single Slot. If the Module is compact (continuous, consistent) then the Parts always hold together (this is secured by the Monoceros WFC Solver). The Module Parts are not accessible individually, the Module is rather presented as a single element.

A Module can consist of a single Part or more Parts. The Parts that are entirely surrounded by other Parts in all 6 orthogonal directions, are not being presented to the user at all. Only the boundary walls of Module Parts are visible and form an orthogonal discrete unit cage of a Module.

#### 1.7.2.2. Connectors
The Module cages are subdivided to match the size of Envelope Slots in their respective directions. The outer walls of a Module cage are considered to be Connectors. Connectors are numbered from `0` to `n-1`, where `n` stands for the number of (outer) Connectors of the respective Module.

The Monoceros Modules are designed to connect to each other through their Connectors. To enable such connection it must be described by a Monoceros Rule. A Connector can occur in multiple Rules, allowing it to connect to multiple counter-Connectors, out of which one will be chosen by the WFC Solver. Each Connector must occur in at least one Monoceros Rule, otherwise the Connector cannot have any neighbor, therefore such Module cannot be placed into the Solution.

Monoceros Rules may allow connection of Connectors from the same Module or from two distinct Modules. A Rule, therefore also a connection, is only valid when the two Connectors are in opposite orientation of the same axis (i.e. negative Y can connect only to positive Y).

Monoceros Rules are referring to Module names and Connector Indices. The supplemental Monoceros components for constructing Rules use visual representation of Connectors (rectangles or dots) to identify the Rule being created.

#### 1.7.2.3. Module Geometry
The purpose of a Monoceros Module is to place geometry into dedicated Slots. The Module data type is not bound to its geometry, which means the shape, size and location of Module geometry has no relation to the Parts of the Module or Slots it may occupy. This is an intentional feature because in real-life architectural and design applications, the Modules will need to posses extending parts (physical connectors) that would not fit the cages of Module Parts and therefore would compromise the WFC solution.

Therefore, the Module Geometry may be small, larger, different than the Module cage or remain completely empty.

Therefore a Module is being constructed from different input data than its Geometry. To mark Module Parts (Slots the Module may occupy) the user specifies Points inside these Parts. For convenience, it is possible to use Slicer helper component, that analyzes input geometry (no matter whether it is the same geometry that is being held by the Module or a different one) and returns exact centers of Slots that may be occupied by Module Parts.

#### 1.7.2.4. Orientation and placement
A Module has a fixed orientation. When it is being placed into Slots it is only being translated (moved) and never rotated. There are maximum of 24 different discreet 90 degree rotations of an object. Rotating all Modules automatically would take a lot of control from the user. Therefore, if any rotation is desired, it has to be done manually by creating a new, different rotated version of the Module. The best way to do this it to rotate and adjust all input data (Module Part Points, Module Geometry) and give it a different name.

#### 1.7.2.5. Module Properties
- **Name** - is a string unique identifier assigned by the user. The Name is automatically converted to lowercase. All Monoceros components with Module list input check if the Module names are unique. If not, they do not compute. The name is the sole identifier of a Module for all purposes, so that it is possible to replace a set of Modules within a solution with a different one.
- **Module Part Center Points** - center Points of Module Parts in cartesian coordinate system. The points are automatically calculated and can be used to create a new Module with the same Parts (cage) or to define the Slot Envelope exactly around the Module.
- **Geometry** - geometry to be placed into Slots with Materialize component.
- **Base Plane** defining Module's coordinate system. The discrete world coordinate system's origin and axial orientation matches that of the Base Plane. For a Module to be compatible with Slots, their Base Planes must match.
- **Module Part Diagonal** - defining Module Part's dimensions in X, Y and Z directions as defined by the Base Plane. For a Module to be compatible with Slots, their Diagonals must match.
- **Connectors** - reveals the Module Connectors as Planes tangent to the Connector rectangle, with Normal pointing outwards and origin at the Connector center. The Connector Indices (or their order in this list) does not represent their direction. 
- **Connector Directions** - unit vectors aligned to the base plane indicating the direction of connector's normal (i.e. positive X direction is always {1, 0, 0}, negative Z is always {0, 0, -1}). Returns a list parallel to the list of Connectors.
- **Connector Use Pattern** - is a boolean list computed from the Module and the list all Rules indicating whether the Connectors have been already described by any Rule. The Monoceros WFC Solver requires each Module Connector to be described by at least one Rule, therefore it is important to generate some Rules for all unused Connectors before attempting to find a solution. Returns a list parallel to the list of Connectors.
- **Is Compact** - single boolean value indicating whether the Module Parts create a coherent compact continuous blob. If there are any gaps or Parts touching only with edges or corners, the Module is not compact. Such Module would not hold together in the WFC solution and therefore is automatically skipped by the Monoceros WFC Solver. It is allowed to construct such Module and to preview it in the viewport, so that the user can manually adjust the input parameters and construct a compact Module.
- **Is Valid** - internally, there are many reasons why a Module could be invalid, but only one reason is allowed to happen in Grasshopper: if a Module consists of too many Parts. The current upper limit is 256 Parts for all Modules combined. If a single Module outreaches this value, it is marked as invalid. It is allowed to construct such Module and to preview it in the viewport, so that the user can manually adjust the input parameters and construct a valid Module.

#### 1.7.2.6. Special Modules: Out and Empty
There are two reserved Module names in Monoceros: **Out** and **Empty**. It is not allowed to manually construct a Module with such name, because they are being constructed automatically. 

The Out Module is present in each solution. It is a Module with a single Part, exactly the size of a single Slot and holds no Geometry. It is automatically placed into Slots outside the user-defined Envelope. All Out Module Connectors are marked with Indifferent Rules, so any Module with an Indifferent Rule marked Connectors can be placed next to it - in other words, it enables the Indifferent modules to be on the boundary of the Envelope. The Out Module does not render in preview, nor bakes.

The Empty Module with a single Part, exactly the size of a single Slot, no Geometry and all Connectors marked with Indifferent Rules has to be constructed manually with a dedicated Monoceros component. It behaves as any other Module because it is a regular Module that can be constructed also without the special Monoceros component. It render in viewport, it can be baked, so that individual Rules can be assigned to it. The Empty Module helps complex Monoceros setups to find a solution, because it fills the gaps between complex Modules and their Parts.

#### 1.7.2.7. Viewport preview and baking
Module preview renders in Rhinoceros viewport with many helper items:
- **Cage** - boundary Connectors of Module Parts render and bake as wire frame rectangles. The Cage is white when the Module is valid, red when it is invalid because it is no compact or consists of too many Parts.
- **Name** - renders as a large text, green if the Module is valid, red it is invalid. The Module Name bakes as a text dot with the Name as a label.
- **Connectors** - render and bake as text dots with Connector Index as a label. The dots are placed in the center of the Connector rectangle. The dots render in colors indicating their direction: 
  - Red = X
  - Green = Y
  - Blue = Z
  - White text = positive orientation
  - Black text = negative orientation
- **Geometry** - renders as regular Grasshopper geometry but does not bake

The helper geometry preview does not follow the Grasshopper convention of a transparent green material for selected items and red for unselected.

The purpose of Module baking is to provide helper geometry and anchors for defining Monoceros Rules. It is possible to snap to Connectors or Cages to define a Rule graphically.

#### 1.7.2.8. Module casts
Module casts help using the Module directly as an input to various components, even when they require a Module name, i.e. Rule or Slot Constructors.
- **Module Name** - is a special Monoceros data type that wraps a string name of a Module. Direct cast to a string is already taken by the user-friendly report, therefore the Module first casts its name to Module Name type, which then casts into text (string) for user-friendly report. Monoceros components, however, expect the Module Name type. This way it is possible to use the Module as an input where a Module Name is expected and there is no need to deconstruct the Module to its properties. 
- **Text** (string) - returns a human-friendly report of the Module's properties in format: `Module "XY" has XY connectors and has XY parts with dimensions XYZ.` and either `The Module is compact.` or `WARNING: The Module is not compact, contains islands and therefore will not hold together.`

### 1.7.3. Rule
Monoceros Rule is a distinct data type describing an allowed adjacency of two Modules by aligning their Connectors facing opposite direction. The Monoceros WFC Solver parses the Slots so that they only allow placement of Modules or their Parts that can become adjacent neighbors according to the Rule set.

Internally, the WFC Solver only works with Explicit Rules, but for convenience Monoceros offers also a Typed Rule. A Typed Rule is automatically unwrapped into one or more Explicit Rules by the WFC Solver and other supplemental components. Both types of Rules manifest as a single data type, can be processed together.

The Rule refers to Modules via their string (text) names and to Connectors via their integer indices. This allows the same Rule set to be used with a different (yet fully compatible) set of Modules.

A single Module Connector can be referred to by multiple Rules but at least one referring Rule is required.

In some cases the Modules cannot connect even though a Rule allows it because their Parts collide. Monoceros does not for check such cases because the WFC Solver itself prevents such situations from happening. That means that even though a Rule is valid, it may never occur in the solution.

#### 1.7.3.1. Explicit Rule
Explicit Rule is closest to the original WFC Rule. It refers to a Connector of one Module that can connector to a Connector of another Module. Its textual representation follows a pattern `module:connector -> module:connector`, i.e. `pipe:1 -> bulb:4`, which translates to: *Module "pipe" can become a neighbor of Module "bulb" if their connectors 1 and 4 touch*.

An Explicit Rule should only allow connection of two non-opposing Connectors, which makes the Rule invalid. Because Connector indices do not indicate their direction, it is only possible to check this when the respective Modules are provided. Therefore a full validity check is performed only when both data is available, most importantly in the Monoceros WFC Solver. When the Explicit Rule is created, it is only checked whether it refers to two different Connectors. 

Explicit Rule is bi-directional, therefore `a:1 -> b:4` equals `b:4 -> a:1`.

##### 1.7.3.1.1. Explicit Rule properties
- **Source Module Name** - is the unique text identifier of the source Module
- **Source Connector Index** - is the unique integer identifier of the source Connector of the source Module
- **Target Module Name** - is the unique text identifier of the target Module
- **Target Connector Index** - is the unique integer identifier of the target Connector of the target Module

##### 1.7.3.1.2. Explicit Rule casts
An Explicit Rule can be cast from a text (string) that has format identical to the user-friendly Explicit Rule text report: `module:connector -> module:connector`.
An Explicit Rule does not casts to any other data type.

##### 1.7.3.1.3. Explicit Rule Viewport preview and baking
An Explicit Rule cannot be displayed on its own. Following a precedent of Vector display component in Grasshopper, there is a Rule Preview component in Monoceros. When provided with all Modules, it displays an Explicit Rule as a line between the connectors described by the Rule. The color of the line indicates the direction of the connectors (and therefore also of the Rule): red means the connectors are facing X direction, green represents Y direction and blue indicates Z direction. 

An Explicit Rule preview can be baked.

#### 1.7.3.2. Typed Rule
Typed Rule is a convenience data type introduced by Monoceros. It assigns a "connection type" to a Connector of one Module, which then can connect to any opposite Connector of any Module with the same "connection type" assigned by another Typed Rule. Its textual representation follows a pattern `module:connector = type`, i.e. `player:1 = jack`, which translates to `
*Module "player" can become a neighbor of any Module if its connector 1 touches the other Module's opposing connector if both connectors are assigned type "jack"*.

A Typed Rule needs to be unwrapped into one or more Explicit Rules before entering the WFC Solver. This is done automatically by the Monoceros WFC Solver component and by supplemental components such as Unwrap Typed Rules or Collect Rules. For Typed Rule unwrapping it is necessary to provide all Modules so that only opposing Connectors of the same type can unwrap into valid Explicit Rules. Even non-opposing Connectors can be assigned the same type. In such case, only valid (opposing) couples will be unwrapped into Explicit Rules.

As the Typed Rule is in fact a half-rule, it is always valid as long as it refers to an existing Module and its Connector.

##### 1.7.3.2.1. Typed Rule properties
- **Module Name** - is the unique text identifier of the (source) Module
- **Connector Index** - is the unique integer identifier of the (source) Connector of the (source) Module
- **Type** - is the unique text identifier of the connection type. Two Modules with opposing connectors assigned the same connection Type can become neighbors.

##### 1.7.3.2.2. Typed Rule casts
A Typed Rule can be cast from a text (string) that has format identical to the user-friendly Typed Rule text report: `module:connector = type`.
A Typed Rule does not casts to any other data type.

##### 1.7.3.2.3. Typed Rule Viewport preview and baking
A Typed Rule cannot be displayed on its own. Following a precedent of Vector display component in Grasshopper, there is a Rule Preview component in Monoceros. When provided with all Modules, it displays a Typed Rule as a line between all couples of opposing Connectors assigned the connection Type. The color of the line indicates the direction of the connectors (and therefore also of the Rule): red means the connectors are facing X direction, green represents Y direction and blue indicates Z direction. In 1/3 (to prevent collision because in real-life use cases lines cross in their middle) of the line there is a dot with a text label indicating the connection Type.

A Typed Rule preview can be baked.

#### 1.7.3.3. Indifferent Typed Rule
For convenience, Monoceros introduces a built-in Rule type: `indifferent`. When a Connector is marked Indifferent, it can connect to any other Indifferent connector of any Module. The purpose of such Typed Rule is to indicate, that the user does not care about specific adjacency of the given Connector and at the same time to satisfy the WFC requirement, to describe each Connector with at least one Rule.

Even the indifferent Rules need to be constructed manually using the [Construct Typed Rule](#1632-typed-rule) or simpler [Indifferent Rule From Point](#17312-indifferent-rule-from-point). In the usual use case, the Indifferent Rule is assigned to those Connectors, that have not been described by any other intentional Rule. For such cases, there is a shorthand constructor component [Indifferent Rules For Unused Connectors](#17313-indifferent-rules-for-unused-connectors).

Just like any other Rule, Typed or Explicit, also the Indifferent Rule can be assigned to a Connector that already is described by another Rule. It can also be used as a disallowed Rule with the [Collect Rules](#1738-collect-rules) component.

## 1.8. Components

### 1.8.1. Slot-related
#### 1.8.1.1. Construct Slot With All Modules Allowed
#### 1.8.1.2. Construct Slot With Listed Modules Allowed
#### 1.8.1.3. Deconstruct Slot
#### 1.8.1.4. Are Slots Boundary
#### 1.8.1.5. Add Boundary Layer

### 1.8.2. Module-related
#### 1.8.2.1. Construct Module
#### 1.8.2.2. Construct Empty Module
#### 1.8.2.3. Deconstruct Module

### 1.8.3. Rule-related
#### 1.8.3.1. Construct Explicit Rule
#### 1.8.3.2. Deconstruct Explicit Rule
#### 1.8.3.3. Is Rule Explicit
#### 1.8.3.4. Construct Typed Rule
#### 1.8.3.5. Deconstruct Typed Rule
#### 1.8.3.6. Is Rule Typed
#### 1.8.3.7. Unwrap Typed Rules
#### 1.8.3.8. Collect Rules
#### 1.8.3.9. Explicit Rule From Curve
#### 1.8.3.10. Typed Rule From Point
#### 1.8.3.11. Rule At Boundary From Point
#### 1.8.3.12. Indifferent Rule From Point
#### 1.8.3.13. Indifferent Rules For Unused Connectors

### 1.8.4. Solver
#### 1.8.4.1. Monoceros WFC Solver

### 1.8.5. Post processing
#### 1.8.5.1. Materialize Slots
#### 1.8.5.2. Assemble Rule
### 1.8.6. Supplemental
#### 1.8.6.1. Slice Geometry

## 1.9. Examples
### 1.9.1. Bare minimum
#### 1.9.1.1. Pseudo code
- construct (one or) more Slots
- construct one or more Modules
- define Rules for each Connector of Each Module
- run Monoceros WFC Solver
- Materialize the result

#### 1.9.1.2. Definition

![Bare minimum](readme-assets/bare-minimum.png)  
![Bare minimum](readme-assets/bare-minimum-screenshot.jpg)
 
 #### 1.9.1.3. Breakdown
 1. [Construct Slot With Listed Modules Allowed](#1712-construct-slot-with-listed-modules-allowed) constructs Monoceros [Slots](#161-slot) that [allows placement of any](#1611-states) Monoceros [Module](#162-module). Input Points collects points placed inside the created Slots. If two of such points are inside the same Slot, such Slot will be constructed twice. Therefore it is advised to deduplicate input points. The Output contains as many Slots as there are input points.
 2. [Construct Module](#1721-construct-module) constructs a single Monoceros [Module](#162-module). Input Name collects unique Module name. Input Points collects points inside Module [Parts](#1621-monoceros-module-parts). The points do not need to be deduplicated. Input Geometry collects geometry that should be [placed](#1624-orientation-and-placement) into respective Slots by the [Materialize Slots](#1751-materialize-slots) component.
 3. [Indifferent Rules For Unused Connectors](#17313-indifferent-rules-for-unused-connectors) generates [Indifferent Typed Rule](#1632-indifferent-typed-rule) for all Module [Connectors](#1622-connectors) from the Input Modules that are not described by any of the [Rules](#163-rule) collected from the Input Rules. In this case there are no preexisting Rules, therefore all Module Connectors will be assigned an Indifferent Rule. The Output contains a single Rule for each unused Module Connector. For more information see the [Indifferent Rules](#181-indifferent-rules) example.
 4. [Monoceros WFC Solver](#1741-monoceros-wfc-solver) parses the [Envelope](#1613-automatic-envelope-wrapping) defined by the Input Slots, according to Input Rules that apply to Input Modules. If successful, the Output contains Slots that are [deterministic](#1611-states) and contain exactly one Module.
 5. [Materialize Slots](#1751-materialize-slots) components places Input Modules' [Geometry](#1623-module-geometry) into Input Slots into which they belong.

### 1.9.2. Defining more Modules and Explicit Rules
#### 1.9.2.1. Pseudo code (almost) without data trees
- construct Slots
- construct each Module individually with one or more Geometry
- define Explicit Rules from Curves
- define Indifferent Rules for unused Connectors
- merge and flatten all Rules
- run Monoceros WFC Solver
- Materialize the result

#### 1.9.2.2. Definition (almost) without data trees
![Without trees](readme-assets/multiple-modules-explicit-rules.png)

#### 1.9.2.3. Pseudo code with data trees
- construct Slots
- graft list of Module names so that each name ends up in a separate branch
- merge Module Part Points and graft so that each Point ends up in a separate branch (if a Module consists of multiple Parts, process the Points like the Geometry in the following steps)
- if one or more Modules should contain more Geometry items, group each Geometry items belonging to each Module (do this also for single Geometry items), then merge to get a list of groups and ungroup so that each list of Geometries ends up in a separate branch
- construct Modules at once using the parallel data trees of Names, Points and Geometries
- flatten the list of Modules
- define Explicit Rules from Curves
- define Indifferent Rules for unused Connectors
- merge and flatten all Rules
- run Monoceros WFC Solver
- Materialize the result

#### 1.9.2.4. Definition with data trees
![Without trees](readme-assets/multiple-modules-tree-explicit-rules.png)
#### 1.9.2.5. Result
![Pitchforks setup](readme-assets/multiple-modules-tree-explicit-rules-a.jpg)
![Pitchforks](readme-assets/multiple-modules-tree-explicit-rules-b.jpg)

### 1.9.3. Indifferent Rules
### 1.9.4. Typed Rules
### 1.9.5. Disallowing Rules
### 1.9.6. Modules with more Parts
### 1.9.7. Module points from Module geometry
### 1.9.8. Empty Module
### 1.9.9. Allowing an Empty neighbor
### 1.9.10. Choosing boundary Modules
### 1.9.11. Slots from geometry
### 1.9.12. Extreme Slot Envelopes
### 1.9.13. Allowing certain Modules in certain Slots
### 1.9.14. Disallowing certain Modules from certain Slots
### 1.9.15. Setting fixed Modules
### 1.9.16. Materializing results
### 1.9.17. Proto-results and custom materialization
### 1.9.18. What makes a good Module
### 1.9.19. Random seed and attempts count
### 1.9.20. Making a valid Envelope

## 1.10. MIT License
Copyright (c) 2021 Subdigital | Jan Pernecky, Jan Toth

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

