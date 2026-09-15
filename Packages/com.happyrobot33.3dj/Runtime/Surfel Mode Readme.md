# 3DJ: The Surfeling
## What is it?

3DJ by default transforms a cube (a highly tesselated one at that) around a set of transformed depth buffers.

Surfels is a new mode to 3DJ that instead of transforming a cube, it transforms a set of quads that represent surface elements from depth.  The existing cube transform method is still intact - all this does is add another mode.

## How does it work?

The surfel variant of 3DJ has three stages: fit, splat, and decode.

### Fit

The fit stage:
* Rejects surfels that might read as "noise" in the final raster.  This is through a sobel filter and a reprojection filter.
* Estimates normal information from the 3DJ depth buffer
* Calculates the texel's footprint to better reduce gaps between surfels later in the pipeline
* Calculates adaptive surfel radius to help preserve details in certain scenarios

Each phase can be enabled or disabled to fit your specific usecase.  Thinks of this as your rejection pass.

### Splat

The splat stage:
* Shears surfels to the generated normal in the fit stage
* Further rejects surfels at a given grazing cut off - this is useful for reducing noise at overlapping points from the 3DJ projection
* Calculates surfel stretch to further boost coverage
* Enables hull rejection - even further sharpening the silouette of the projected avatar, and acting as a "last ditch" surfel rejection mechanism

Similar to the fit stage, each phase can be enabled or disabled.  Enabling or disabling these provide different performance profiles for whatever your target hardware is.

### Decode

The decode stage works like this:
1. Take a "surfel mesh" which is a representation of the total depth resolution.
  - These get dense, and the surfel version of 3DJ works around this in the editor through the SurfelRenderComponent - if you're doing manual setup, it'll add the necessary components.
2. Transform the surfel mesh in the vertex shader
  - Takes the surfel mesh, transform each surfel based upon the provided depth data
  - Any surfels that have empty or invalid data are moved out of clip space to avoid rasterization
    - This is more or less the universally supported way of telling the GPU "don't draw this" - though it _might_ be faster on desktop GPUs to collapse into a degenerate triangle.  I have not yet profiled this.
3. Color surfels using the 3DJ color buffer
4. Mask surfels using the meta pass's hull and weight
  - Both come straight out of the splat stage, so the fragment shader never has to go looking at the other faces itself.  Weight scales the surfel's alpha, hull cuts it off entirely.
5. Shape each surfel in the fragment shader
  - Every surfel is a quad, and a falloff curve turns that quad into a disc.  There's also a rectangular mode if you'd rather keep the whole quad.
  - Coverage outside the footprint is clipped, so overlapping surfels only ever blend where they actually overlap.
6. Blend the surfels together
  - Two pass is the default: a depth prepass dithers the edges of every surfel and marks the stencil, then the color pass blends only where the prepass drew.  This is what gives soft interiors with a hard silhouette.
  - Z-write and cutout are the cheap opaque options, dither is the cheap "soft" option.
  - Accumulate does a proper weighted average through a grab pass.  It looks the best and it also costs the most - it is not an option on Quest.
  - The dither itself is bayer with an optional blue noise layer on top.  Blue noise is one of the static features, so if you don't want the texture read you can compile it out.

Like the other two stages, everything in the decode stage is driven by the material.  The in world tweak panel writes to those same materials, so anything you can set in the inspector you can also set from inside the world.

## How do I use this?

### Prefab approach (do this)
If you're using the manager prefab it should be pretty drop in.  The manager prefab was updated with the surfel stuff.

Note that you will now have **two** renderers - the cube based renderer from standard 3DJ and the new surfel renderer.  This is useful for tweaking and tuning, but ultimately you should pick one and disable the other.

### Manual approach (seriously, just take the prefab approach if you can)

However, if you're doing manual setup you will need to:
1. Have a 3DJ manager in the scene - the surfel setup reads everything it needs from it, and does nothing without one.
2. Create an empty game object
3. Add SurfelRenderComponent

SurfelRenderComponent's editor scripts should set everything up for you the moment you add it: the materials, the render textures, and the surfel mesh at the manager's depth resolution.  The shaders use 3DJ's globals, so it should "just work".

What it does **not** do on its own is take over as the manager's playback object.  That's the "Assign as Manager playback object" button on the component.  Until you press it the manager doesn't know about the surfel renderer. The shaders still respect global playback on their own, but local playback won't hide it, and the cube stays active.  Pressing it is the "pick one" from above.

If you want the in world tweak panel, that's a separate step: the panel is a Canvas you create yourself.  Add SurfelPassViewController to it, assign the fit, splat, and decode materials (the three under Runtime/Playback/Surfel/Generated), and press "Build controls" in its inspector.  It needs the SurfelRenderComponent to have run its setup first so those materials exist.

## What do all the knobs do?

Three materials, one per stage.  The toggles and dropdowns are static - set them on the material and they compile in or out.  Everything else is live and shows up on the tweak panel.

### Fit (SurfelPlaybackMeta)

#### Sobel

Reject depth edges entirely.

* Sobel Threshold
  * How hard an edge counts

#### Reprojection reject

Drop texels other faces contradict.

* Reproject tolerance
  * Contradiction slack, in meters
* Contradicting faces to reject
  * How many faces must disagree

* Fit window radius
  * Plane fit neighborhood size

#### Adaptive radius

Scale surfels by surface detail.

* Radius min
  * Multiplier where detail is high
* Radius max
  * Multiplier where surface is flat
* Detail scale
  * Residual that means "detailed", meters

### Splat (SurfelPlaybackMetaSplat)

* Seam blend
  * Fade in where faces overlap
* Grazing cutoff
  * Drop surfels seen edge on

#### Shear to normal

Tilt discs to the surface.

* Max stretch
  * Cap on tilted disc elongation

#### Hull

Reject surfels outside the silhouette.

* Hull threshold
  * How much hull counts as inside
* Hull feather
  * Soften the hull edge

### Decode (SurfelPlaybackDecode)

* Splat radius
  * Global surfel size multiplier
* Stride
  * Texels per surfel, matches the bake
* Ignore Global Playback Control
  * Draw even when playback is off
* Lock Position
  * Ignore the recorded position
* Lock Rotation
  * Ignore the recorded yaw

#### Blend mode

Two pass, z-write, dither, cutout, or accumulate.

* Depth offset
  * How far the prepass pushes back (Two pass, accumulate)
* Cutout
  * Alpha test threshold (Cutout, and the prepass hull edge in two pass and accumulate)
* Accumulation scale
  * Weight range for accumulate mode (Accumulate)

#### Falloff

How fast a disc fades out.

* Shape
  * Disc or the whole quad
* Falloff Multiplier
  * Overall alpha boost
* Use falloff texture
  * Texture instead of the curve

#### Fade

Fade out close to the camera.

* Fade distance
  * How far the camera fade reaches
* Fade multiplier
  * How hard it fades up close

#### LOD

Thin surfels with distance, halving each time it doubles.

* LOD distance
  * Where the first halving starts

#### Dither

Bayer, bayer plus blue noise, or fractal.  Applies in dither mode, and to the prepass edges in two pass and accumulate.

* Blue noise mix
  * How much blue noise over bayer.
* Blue noise jitter
  * Animate the blue noise.
* Dot Scale
  * Fractal dot size on screen.
* Dot Size Variability
  * Dots change count or size.
* Dot Contrast
  * How crisp the dots are.
* Stretch Smoothness
  * Smoothing on stretched dots.
* Exposure
  * Brighten before dithering.
* Offset
  * Shift before dithering.