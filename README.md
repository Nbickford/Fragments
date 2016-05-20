# Fragments
Code samples from interesting projects.

------

### GLSL...

*... distance fields, and real-time path tracing:* [**Theme From Brazil**](https://www.shadertoy.com/view/MtSGzW) started out as a real-time theatrical lighting test, but soon turned into a neat sort of mini-demo.

*... and applied bokeh:* [**Nyan Infinity**](https://www.shadertoy.com/view/lt2GWV) just renders defocused Nyan Cat quads in an ambient space.

*... and gravitational rendering:* [**Earth Not Above**](https://www.shadertoy.com/view/4s3SzN) implements a version of the code Double Negative and Kip Thorne used to render the black holes in Interstellar, but does it in a real-time pixel shader. And, for what it's worth, somebody liked the code! Comes with a [documentation paper](http://bit.ly/254or0K).

### C#...

*...and signal processing:* **Analytical DJ** (/analytical/Analytical/Program.cs) is a 	program I wrote over a short weekend in 2014 that finds mashups of audio files by breaking them down into individual beats (with the help of Queen Mary University's *Sonic Annotator*), analyzing how each beat sounds, then compares songs to determine how well they fit together.

The results actually really aren't too bad; check out https://soundcloud.com/analytical-2 for a few examples.

------

*...and the XNA Framework:* **Doughboy:Assault** (/doughboy) is a game I made with the fantastic Kris Owen (art) and Sean Mortensen (music) in early 2013, and an example of one of the larger projects I've worked on. The game itself is a sort of two-player tower defense, but (as with many things) it's slightly more complicated than that.

Check out http://doughboy.neilbickford.com/doughboy_1_0_1.zip for a neat playable version; you'll probably want a copy of the [.Net 4.0 Redistributable](http://www.microsoft.com/en-us/download/details.aspx?id=17851) as well as the [XNA Framework, version 4.0](http://www.microsoft.com/en-us/download/details.aspx?id=20914).

------

### ...and Mathematica.

**Special and Limiting Values of the Dedekind Eta Function** (/dedekind-eta) contains code which solves (given enough time) the century-old problem of determining the exact symbolic value of the Dedekind eta function at an imaginary surd (a number of the form $\sqrt{-n}$, where $n\in \mathbb{Z}^+$). It's likely one of the most technical pieces of code I've ever written, but at the end of the day it reduces to a few different kinds of searches and one or two novel data structures. Check out the documentation!

*-NB, Spring 2016.*