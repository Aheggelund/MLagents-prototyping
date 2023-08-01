# MLagents-prototyping

Prototyping a RL model to search for, find and interact with a target in 3D space using the Unity MLAgents framework.
The scripts in this repo are used in the following way:

## Goal.cs
attached to the target the agent should learn to locate and interact with

## Wall.cs

Attached to the boundaries of the training environment so that the agent will learn to stay within bounds

## MoveToGoalAgent.cs

Used for controlling the agent through heuristics or train the model using inference. Sets up rewards, as well as penalties.
In order for this to work the Agent needs a RaycastSensor3D object attached to it, preferably with an angle spread less than 70'

## PartGoal.cs

Not yet implemented.

## 
