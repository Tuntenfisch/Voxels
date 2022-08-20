https://user-images.githubusercontent.com/11965324/131001408-63d36fde-0c7a-4d63-8e8c-82461c02af72.mp4

# Voxels

GPU-based implementation of [Dual Contouring](https://www.cs.rice.edu/~jwarren/papers/dualcontour.pdf) in Unity for destructible voxel terrain.

The core features are:
- Infinite procedural world
- Seamless chunked level of detail
- Multiple materials with smooth material transitions
- Graph-based terrain generation using [XNode](https://github.com/Siccity/xNode)

**Note:** Sharp voxel terrain features are preserved, not by solving the [QEF](https://en.wikipedia.org/wiki/Mean_squared_error) as described in the paper above, but by using the [Schmitz Particle method](https://www.inf.ufrgs.br/~comba/papers/thesis/diss-leonardo.pdf#page=42) instead, which is much easier to implement and supposedly faster.

**Note:** The Unity version used is 2021.1.16f1.

**Note:** This repository uses [Git Large File Storage](https://github.com/git-lfs/git-lfs/wiki/Tutorial). If you want to clone this repository, make sure you have Git Large File Storage installed on your PC.

## Materials
The voxel terrain can be made up of a practically infinite amount of different materials. Materials are defined via a simple enumeration called [```MaterialIndex```](/Assets/Scripts/Voxels/Materials/MaterialIndex.cs). Since the implementation is leveraging the GPU for generating the mesh, some structures have been defined multiple times, once in C# and once in HLSL. This is also
the case for the material index. The HLSL implementation can be found [here](/Assets/Compute/Voxels/Include/Material.hlsl).

Each material can be assigned a different texture through a [ScriptableObject](/Assets/Scripts/Voxels/Materials/MaterialConfig.cs):

![Material_Config](https://user-images.githubusercontent.com/11965324/131109618-e9016c6b-f9aa-46a3-8cba-f50279ddb899.png)

The material index of a voxel is encoded as an unsigned integer and is used later on (as a custom vertex attribute) to render the generated mesh with the correct textures. The material index can be queried from [scripting](https://github.com/Tuntenfisch/Voxels/blob/release/Assets/Scripts/World/WorldManager.cs#L128), as well.

## Texturing

Texturing of the voxel terrain is done with a [custom URP PBR shader](/Assets/Shaders/Voxels/Voxel.shader) and supports:
- Albedo mapping
- Normal mapping
- Metallic mapping
- Occlusion mapping
- Height mapping
- Smoothness/roughness mapping

Since the procedurally generated terrain doesn't have any UV coordinates, [Triplanar mapping](https://catlikecoding.com/unity/tutorials/advanced-rendering/triplanar-mapping/) is used to apply the various textures.

**Note:** I'm using a custom shader because Unity's shader graph doesn't support a geometry pass, which I need to enable smooth transitions between different voxel materials.  

**Note:** Textures are taken from https://ambientcg.com/ and fall under the [Creative Commons CC0 1.0 Universal License](https://creativecommons.org/publicdomain/zero/1.0/).

## Smooth Material Transitions

One challenged I faced was getting different material textures to smoothly blend between each other. One approach would be to use something like a splat map to support multiple materials.

**Note:** [Catlike Coding's "Rendering 3"](https://catlikecoding.com/unity/tutorials/rendering/part-3/) provides an introduction into those if you don't know what they are.

But there are a number of issues with splat maps:
- Only supports up to 5 different textures. Either I limit the voxel terrain to only 5 different materials (way too few) or I somehow ensure that each terrain chunk doesn't have more than 5 different materials in it and do some sort of on the fly texture switching to render the correct material textures for each chunk?!
- Can't exactly use a 2D texture as a splat map for voxel terrain. So maybe something like a 3D texture could work?

To be honest, just the first bullet point above was enough for me to not further pursue this approach. I wanted something that supported a theoretically unlimited amount of materials in a single chunk. 

### Hard Transitions

Achieving hard material transitions is pretty easy: Each of my vertices has a material index. To ensure that any given triangle, which consists of 3 vertices each, has only one specific material index associated with it. I simply need to duplicate certain vertices on material boundaries while generating the mesh with dual contouring. Here's an example:

![image](https://user-images.githubusercontent.com/11965324/131113079-7668de4f-f014-4b4a-a850-d203475eb16d.png)

Essentially, the red and blue triangles represent two materials which are part of the same mesh. The green vertices are on the boundary between both materials and need to be duplicated with the vertex attribute for the material index assigned once to the red and once to the blue material. Detecting these boundaries is fairly easy, during triangulation of a triangle (which is done on a per cell basis) check if the triangle's neighbouring vertices have the same material index as the current cell vertex you're working on. If they don't, just duplicate the neigbhouring vertices, assign the same material index to the duplicates, and make up the triangle of your current cell's vertex and the neighbouring duplicates.

During shading, you use the vertex's material index as a lookup into a [```2DTextureArray```](https://docs.unity3d.com/ScriptReference/Texture2DArray.html) - which you populated with the corresponding textures for each material, beforehand - and apply whichever texture you've got out of the lookup to the triangle. This approach can be implemented in Unity's shader graph, too.

### Smooth Transitions

But to get smooth transitions between textures of different materials you need two things for each fragment/pixel you want to shade:
- The three (potentially different) material indices of the triangle.
- Three weights that (for each pixel of the triangle) give you the amount each vertice's material is active. This weight should then smoothly blend between the triangles vertices based on the fragment/pixel you're working on.

This is where the geometry shader stage comes into play (and also why I can't use Unity's shader graph). The geometry shader stage runs after the vertex shader stage and before the fragment/pixel shader stage. It let's me work, not on individual vertices, but on (in my case) triangles as a whole, i.e. a set of 3 vertices. 

**Note:** If you're unfamiliar with the geometry shader stage you can get an introduction [here](https://gamedevbill.com/unity-vertex-shader-and-geometry-shader-tutorial/). One important thing to know tho, is that for each triangle the geometry shader stage works on the original vertices from the vertex shader stage, meaning modifying one vertex in the geometry shader stage doesn't affect any neigbhouring triangles which also share that vertex. It's like having a mesh where each triangle has it's own vertices, i.e. a flat shaded mesh.

My geometry shader stage is largely a passthrough stage, meaning I have 3 vertices as an input and I output 3 vertices, too. The inputs are the 3 vertices making up a triangle. Each of these vertices has a material index associated with it. 

For each triangle passed into the geometry shader stage, I gather the 3 material indices of the triangle's vertices and construct a ```uint3``` where the first material index is assigned to the first entry of the vector, the second to the second entry and so on. Each vertex I output gets this ```uint3``` assigned. 

This alone doesn't enable smooth transitions yet, tho. Additionally each vertex I output gets a ```half3``` called ```materialWeights``` assigned as follows:

* The first vertex gets the vector ```(1, 0, 0)``` assigned.
* The second vertex gets the vector ```(0, 1, 0)``` assigned.
* The third vertex gets the vector ```(0, 0, 1)``` assigned.

Between the geometry shader stage and the final fragment/pixel shader stage these material weights will get interpolated based on the position of the current fragment/pixel inside the triangle. 

In addition with the material indices, which are also provided by my geometry shader stage (as explained), I can then sample a ```TEXTURE2D_ARRAY``` 3 times (using each material index as the index into the array once) and add those texture samples together using the material weights to arrive at the final color.

**Note:** The material indices are not interpolated between the geometry and fragment/pixel stage!

## Terrain Generation

Terrain generation can be configured through a graph-based editor:

![Generation_Graph](https://user-images.githubusercontent.com/11965324/131109547-9a2bf3f9-ce7a-4aa5-9e97-18fbd69ffbf0.png)

Features include:
- [Fractional Brownian Motion](https://iquilezles.org/www/articles/fbm/fbm.htm) nodes
- [Domain Warping](https://iquilezles.org/www/articles/warp/warp.htm) nodes
- [CSG](https://en.wikipedia.org/wiki/Constructive_solid_geometry)  primitive nodes, namely a cuboid and a sphere primitive node.
- Material nodes.
- CSG operation nodes to combine FBM noises and CSG primitives.
- Transform nodes to translate, scale, rotate FBM noises and CSG primitives.

**Note:** [Keijiro's Noise Shader Library](https://github.com/keijiro/NoiseShader) is used to generate simplex noise on the GPU.

**Note:** See this [question](https://gamedev.stackexchange.com/questions/193938/how-to-evaluate-a-binary-expression-tree-in-hlsl-without-recursion-or-a-stack) for more insight on how I evaluate the graph to generate the terrain on the GPU.

## Level of Detail

![LOD](https://user-images.githubusercontent.com/11965324/131109509-18c113d0-4fba-4c01-9ab5-d058440f7812.png)

The implementation for LOD is inspired by [Sebastian Lague's video "Procedural Landmass Generation (E21: fixing gaps)"](https://www.youtube.com/watch?v=c2BUgXdjZkg). Just like in the video, the skirts of a chunk are always generated at the highest level of detail, ensuring that there are no gaps between different level of details.
The actual implementation obviously differs quite a lot since I'm not dealing with heightmap terrain.

## Why publish it for free?

I spent quite some time on implementing various voxel terrain algorithms (Marching Cubes, Cubical Marching Squares, Dual Contouring) and noticed during the process that although there are implementations online, they often lack features like level of detail or multiple material support.

Furthermore, getting voxel terrain to be performant can be quite hard although I'm sure my implementation can be improved, too.
That being said, my whole GPU-based approach might be suboptimal in the first place since I have to read back the mesh to the CPU.

In the end I kind of lost motivation to continue working on this project and decided to move on. The project is nowhere near feature-complete to be released as an asset. Besides, I do not want to deal with providing customer support. Releasing it for free was basically the easiest way of getting it out to the public.

I hope - by making this publicly available - I can help some people who are interested in voxel terrain.
