# Clonecraft 4

<img align="right" src="https://github.com/user-attachments/assets/0b6ece30-db37-48e7-ac84-515254cdcf13">

Clonecraft 4 is a voxel-based game built in Unity, combining fractal Perlin/Simplex noise to generate 2 biomes, networking and skins to make a fun and interactive demo.

## ✨ Features

[x] **Voxel World**: Explore a fully interactive, procedurally generated voxel world.
[x] **Mesh Optimization**: Efficiently render voxel data with optimized meshes. Uses a texture atlas to reduce number of draw calls to the GPU aswell.
[x] **Chunk Serialization & Compression**: Utilize running length encoding (RLE) to serialize and compress chunk data, reducing network overhead.
[x] **Real-Time Networking**: Send and receive optimized chunk data through Socket.IO for seamless multiplayer experiences.
[x] **Dynamic Player Skins**: Automatically fetch and apply player skins from the Minecraft skins website.
