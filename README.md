[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/ormJifKv)
# Games and Artificial Intelligence Techniques (COSC2527/3144)<br>Semester 1 2025 - Assignment 3

**Student1 ID:** \*\*\*s3968971\*\*\* <br>
**Student1 Name:** \*\*\*Paul Johny Mampilly\*\*\* <br>
**Student1 Contribution:**\*\*\*Developed player movement, shooting, health bar, and death animation logic,Implemented and trained `PlayerAgent` using Unity ML-Agents PPO algorithm, Created and tuned `PlayerBehaviour` YAML config for ML training, Built helper AI logic that detects zombies, rotates, and shoots autonomously, Wrote `AIHelperSpawner.cs` to spawn a helper when player presses space, lasting 5 seconds, Integrated helper into the gameplay loop and ensured only one is active at a time.
\*\*\* <br>

**Student2 ID:** \*\*\*s3918525\*\*\* <br>
**Student2 Name:** \*\*\*Philip Kim\*\*\* <br>
**Student2 Contribution:**\*\*\*Implemented and trained zombie agent using Unity ML-Agents, integrated the zombie agent's game behavior (health, movement, attack behaviors), and set up a wave-based zombie spawner with collision-aware placement\*\*\* <br>

**Student3 ID:** \*\*\*s4005338\*\*\* <br>
**Student3 Name:** \*\*\*Christian Nieves\*\*\* <br>
**Student3 Contribution:**\*\*\*Worked on procedural generation/terrain and also worked on zombie variant ML\*\*\*

This is the README file for Assignment 3 in Games and Artificial Intelligence Techniques.

Note:

* The starter project was created in Unity 6000.0.37f1. Please use the same Unity version for your submission.
* Please do not edit the contents of the .gitignore file.

Instructions:
- Open this file in unity to access the game
- "Survival" Scene is the main scene with "zombie spawner" activated to spawn zombies around player
- "Terrain" is a test generation file.

Saved models:
- ZombieBrain.onnx zombie trained file in /Assets
- BruteZombieBrain.onnx brute zombie trained file in /Assets
- PlayerBehaviour.onx trained file in /Assets

<br>
Description of Contributions and Commit ID:
Paul
- Player movement and animation system: CommitIDs (60e592a, 6f9825e)
- Shooting mechanics : CommitIDs (d9e6031)
- Health and death system: CommitID (eaa2e24, f3535b1)
- AI helper training, AI spawning system and YAML training configuration: CommitIDs (d15313d, 35a7539)

Philip
- Wave-based zombie spawner: CommitIDs (24320f7, e238c8a)
- Train zombie agent using Unity ML-Agents: CommitIDs (5d1cc85, b249270, fb046af, f68df4a)
- Zombie agent's game behavior: CommitID (5d1cc85)

Christian
- Terrain generation: CommitIDs (723e1347, 2f91e120)
- Brute Zombie ML: CommitID: (b0c06381)