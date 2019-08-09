FAST SOFTPARTICLE
VisualWorks
----------

[Imgur](https://i.imgur.com/SyuiSlb.jpg)

The Fast Softparticle Package might make a soft edge on the particle where the hard-edge occurs. (aka Soft-particles)

KEY IDEA
----------

[Imgur](https://i.imgur.com/Fg8cLgq.png)

Detect one of the bottom planes of the particle.
Fade alpha based on distance from the plane.

KEY FEATURES
----------

* NO G-buffer is used.
 * Mobile friendly
 * Forward rendering-pipeline friendly
* It uses vertex-streams for fade information per particle.
* Collision detection for fading is computed in a scene.
 * Per Particle-playing
 * Per a particle-system
 * Per Particle
* The changed shader is small and too cheap.




