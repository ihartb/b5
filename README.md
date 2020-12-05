TODO IN PRIORITY ORDER:
 - [ ] the two diverge points (check the narrative below) just dont work with user input bc selector parallel keeps giving errors!!!!
 - [ ] camera angles
 - [ ] is random selector really a new control node?? 
 - [ ] apple IK doesn't stay in finger tips, and doesnt drop on stop interaction
 - [ ] make fight sequence better? 
 - [ ] escape arc could be better maybe idk

 - [x] Story: prison escape; each of the individual story points coded.
   - Story itself has start and diverge point
     - Start includes...
       - [x] jailor presses button and opens cell door
       - [x] prisoner leaves cell
       - [x] prisoner jailor walk to cafeteria
       - [x] prisoner grabs food and sits down to eat
       - [x] guards get distracted
     - DIVERGE once guards are distracted prompt prisoner to leave or stay...
       - [x] escape option can lead to random failure or success
       - [x] DIVERGE2: stay option prompts user again to stay or escape
         - [x] stay -> go back to cell
         - [x] escape -> can lead to random failure or success 
   - [x] dialogue UI working
   - [x] prompt user to escape or stay
   - [x] stayarc method works
   - [x] required to have multiple story endings
 - [ ] complex environment: adapt from previous assignment
 - [x] 3 characters
 - [-] 2 new affordances (interaction between characters and/or objects)
 - [-] 2 IK affordances
 - [x] behavior tree using KADAPT library
 - [-] create specific control node and assign it a behavior
   -[x] randomly select one from list, might be like selectorshuffle tho, idk i cant think of another type 
 - [-] interactive behavior tree for the story
   - two points in the narrative we get this issue, does not work with selector parallel as it should, check script demobt for example which does work...yet ours doesnt??? idk 
   - [ ] include drawing in report
   - [ ] attach human player controls to one character

TIPS:
- find all possible actions / gestures that can be performed here:
	- b5/Assets/Core/Scripts/Character/BehaviorMecanim.cs
	- b5/Assets/Core/Scripts/Character/CharacterMecanim.cs
	- b5/Assets/Core/Scripts/Character/BodyMecanim.cs


