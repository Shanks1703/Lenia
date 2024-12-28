# Lenia in Unity

Lenia is a continous cellular automata as opposed to Conway's Game of Life which operates in a discrete space.
Cells samples neighborhood pixels using a convolution kernel, the behaviour of the cellular automata is defined by the kernel shape and μ/σ parameters.
This implementation runs on GPU using compute shaders.

![](https://github.com/Shanks1703/Lenia/blob/main/Lenia.gif)

> [!NOTE]
> Large kernel sizes (>128) will affect performances since the kernel averaging is done with two nested loops.

> [!NOTE]
> This implementation may be wrong or incomplete, feel free to update it.
