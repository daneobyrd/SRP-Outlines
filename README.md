# SRP-Outlines

This is a **Unity 2021.3.2f1** project using the **Universal Render Pipeline** (v. 12.1.6) and utilizing features of the Scriptable Render Pipeline to create a fullscreen outline effect.

Compute shaders are located in `Assets/Resources`.
Scriptable Render Passes are located in `Assets/Plugins/RenderPass`.

Most recent work is in the `dev` branch.

Currently there are two filters being used for edge detection:

• Laplacian
• Frei-Chen

Resources:
https://cse442-17f.github.io/Sobel-Laplacian-and-Canny-Edge-Detection-Algorithms/
https://www.rastergrid.com/blog/2011/01/frei-chen-edge-detector/