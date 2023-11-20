# TV Lighting - Unity URP

An example project for how to accurately simulate a TV screen playing video in Unity. Built using the universal render pipeline but should work just as well in HDRP.

Free to use, but credit is appreciated. Please let me know if you find any optimizations or improvements! PRs are welcome.

Latest verified editor version: 2022.3.2f1

## Samples
![TV Screen Sample Video - Color Test](/repository/Samples/sample-vid-ct.gif?raw=true "Sample Video Color Test")
![TV Screen Sample Video - Movie](/repository/Samples/sample-vid-movie.gif?raw=true "Sample Video Movie")


## Instructions
The sample scene provided has a number of TV screens that each play different kinds of videos. You can enable them individually to see their different configurations. You should be able to easily switch in your own videos to the Video Player component of the TV.

### Config Options
- The TV Controller holds most of the config. All of the fields have descriptions visible in the Inspector.
- To affect the bloom of the screens, change the "Video Emission Intensity" property of the screen material. You can also change this globally via the bloom setting in the post processing volume.
- You can optionally add an overlay texture for the screen in the screen material.
- You can change the ambient color of the screen in the screen material. Increase or decrease the intensity of the color to change how much it effects the emission.

## Known Limitations
- This TV does not seek to perfectly simulate LED screens. It was designed to be a simpler, more computationally friendly option. This means that under close inspection, the screen will not show the individual RGB diodes of a traditional LED display, and the lighting emissions of the screen are simplified to a single color.
- Because the lighting is changing each frame, the screen must use realtime lighting, which means we cannot render light bounces off of surfaces. (Unity only allows light bouncing for baked lighting.)

## Bugs
- The screen lighting does not change when building and running the game. It works fine when running via IDE though. I suspect it has something to do with the RenderTexture in the TVController.


## Acknowledgements
vintage TV model: https://www.cgtrader.com/free-3d-models/electronics/video/vintage-tv-a4f79508-a594-4119-a80f-e3151c855af3

cardboard box model: https://polyhaven.com/a/cardboard_box_01

wood floor texture: https://polyhaven.com/a/herringbone_parquet

beige wall texture: https://polyhaven.com/a/beige_wall_001

fiber wall texture: Unknown