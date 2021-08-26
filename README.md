![Voxels](/Images/Voxels.png?raw=true)

# Voxels

GPU-based implementation of the [Dual Contouring algorithm](https://www.cs.rice.edu/~jwarren/papers/dualcontour.pdf) in Unity to enable destructible voxel terrain.

The core features are:
- Seamless chunked level of detail
- Multiple materials with smooth material transitions
- Graph-based terrain generation using [XNode](https://github.com/Siccity/xNode)

**Note:** Sharp voxel terrain features are preserved, not by solving the [QEF](https://en.wikipedia.org/wiki/Mean_squared_error) as described in the paper above, but by using the [Schmitz Particle method](https://www.inf.ufrgs.br/~comba/papers/thesis/diss-leonardo.pdf#page=42) instead, which is much easier to implement and supposedly faster.

## Materials
The voxel terrain can be made up of a practically infinite amount of different materials. Materials are defined via a simple enumeration called [```MaterialIndex```](/Assets/Scripts/Voxels/Materials/MaterialIndex.cs). Since the implementation is leveraging the GPU for generating the mesh, some structures have been defined multiple times, once in C# and once in HLSL. This is the case for the ```MaterialIndex```. The HLSL implementation can be found [here](/Assets/Compute/Voxels/Include/Material.hlsl).

Each material can be assigned a different texture through a [ScriptableObject](/Assets/Scripts/Voxels/Materials/MaterialConfig.cs):

![Material Config](/Images/Material_Config.PNG?raw=true)

The material index of a voxel is encoded as an unsigned integer and is used later on (as a custom VertexAttribute) to render the generated mesh with the correct textures. The material index can be queried from [scripting](https://github.com/Tuntenfisch/Voxels/blob/release/Assets/Scripts/World/WorldManager.cs#L128), as well.

## Texturing

Texturing of the voxel terrain is done with a custom URP PBR shader and supports:
- albedo mapping
- normal mapping
- metallic mapping
- occlusion mapping
- height mapping
- smoothness/roughness mapping

Since the procedurally generated terrain doesn't have any UV coordinates, [Triplanar mapping](https://catlikecoding.com/unity/tutorials/advanced-rendering/triplanar-mapping/) is used to apply the various textures.

I'm using a custom shader because Unity's shader graph doesn't support a geometry pass, which I need to enable smooth transitions between different voxel materials.  

**Note:** Textures are taken from https://ambientcg.com/ and fall under the [Creative Commons CC0 1.0 Universal License](https://creativecommons.org/publicdomain/zero/1.0/).

## Terrain Generation

Terrain generation can be configured through a graph-based editor:

![Generation Graph](/Images/Generation_Graph.PNG?raw=true)

Features include:
- [Fractional Brownian Motion](https://iquilezles.org/www/articles/fbm/fbm.htm) nodes
- [Domain Warping](https://iquilezles.org/www/articles/warp/warp.htm) nodes
- [CSG](https://en.wikipedia.org/wiki/Constructive_solid_geometry)  primitive nodes, namely a cuboid and a sphere primitive node.
- Material nodes.
- CSG operation nodes to combine FBM noises and CSG primitives.
- Transform nodes to translate, scale, rotate FBM noises and CSG primitives.

**Note:** [Keijiro's Noise Shader Library](https://github.com/keijiro/NoiseShader) is used to generate simplex noise on the GPU.

**Note:** See this [question](https://gamedev.stackexchange.com/questions/193938/how-to-evaluate-a-binary-expression-tree-in-hlsl-without-recursion-or-a-stack) for more insight on how I evaluate the graph to generate the terrain.

## Level of Detail

The implementation for LOD is inspired by [Sebastian Lague's video "Procedural Landmass Generation (E21: fixing gaps)"](https://www.youtube.com/watch?v=c2BUgXdjZkg). Just like in the video, the skirts of a chunk are always generated at the highest level of detail, ensuring that there are no gaps between different level of details.
The actual implementation obviously differs quite a lot since I'm not dealing with heightmap terrain.