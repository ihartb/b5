using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using TreeSharpPlus;
using RootMotion.FinalIK;
using UnityEngine.UI;

public class MyBehaviorTree : MonoBehaviour
{
	//agents
	public GameObject jailor;
	public GameObject prisoner;
	public GameObject guard;

	//wander points
	public GameObject wanderPoints;
	public Transform cellDoorButtonWP;
	public Transform outsideCellWP;
	public Transform cafeteriaGuardWP;
	public Transform cafeteriaSeatWP;
	public Transform distractJailorWP;
	public Transform escapeRouteWP;
	public Transform prisonerCellWP;
	public Transform outsideCafeteriaWP;
	public Transform fightJailorWP;
	public Transform fightGuardWP;

	//objects
	public GameObject apple;
	public GameObject cellDoor;
	public GameObject cellDoorButton;
	public Transform cafeteriaTable;

	//IK
	public InteractionObject cellButtonIK;
	public InteractionObject appleIK;
	public InteractionObject shakePoint;
	public GameObject shakePoint_obj;
	public InteractionObject shakePoint2;
	public GameObject shakePoint2_obj;
	public FullBodyBipedEffector rightHandIK;
	public FullBodyBipedEffector rightHandIK2;

	//UI
	public Text dialogue;

	// character control
    public GameObject target;
    public bool canMove = true;
    private bool targetSelected = true;
	private bool nodeIsRunning = true;

	private BehaviorAgent behaviorAgent;
	private int userInput = 0;
	public enum StoryArc
	{
		NONE,
		ESCAPE,
		STAY,
		LATEESCAPE,
		ESCAPEFAIL
	};
	public StoryArc currArc = StoryArc.NONE;
	private float timeToDecide = 0;



	// -2 not there yet in story, -1 waiting for reply, 0-n answer
	private int midStoryWait = -2;

	// Use this for initialization
	void Start ()
	{
		behaviorAgent = new BehaviorAgent (this.BuildTreeRoot ());
		BehaviorManager.Instance.Register (behaviorAgent);
		behaviorAgent.StartBehavior ();
		target.transform.position = prisonerCellWP.position;

	}

	// Update is called once per frame
	void Update()
	{
		if (Input.GetMouseButtonDown(1))
        {
            //select target
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray.origin, ray.direction, out hit))
            {
                target.transform.position = hit.point;
            }
            targetSelected = true;
        }
	}

	//control node
	// private Node RandomSelector(params Node[] children)
	// {
	// 	int i = UnityEngine.Random.Range(0, children.Length);
	// 	return children[i];
	// }
	// private Node BoolSelector(Func<bool> condition, params Node[] children)
	// {
	// 	print(condition().ToString());
	// 	if (condition()) return children[0];
	// 	else return children[1];
	// }

	//helper action methods
	private Node ST_Approach(GameObject participant, Transform target)
	{
		Val<Vector3> position = Val.V (() => target.position);
		return participant.GetComponent<BehaviorMecanim>().Node_GoTo(position);

	}

	private Node ST_SitDown(GameObject participant)
	{
		return (
			new LeafInvoke(() =>
			{
				return participant.GetComponent<BehaviorMecanim>().Character.SitDown();
			})
		);
	}

	private Node ST_StandUp(GameObject participant)
	{
		return (
			new LeafInvoke(() =>
			{
				return participant.GetComponent<BehaviorMecanim>().Character.StandUp();
			})
		);
	}

	private Node ST_ShakeHands(GameObject participant, GameObject participant2)
	{
		return ( new Sequence(
				new LeafInvoke(() => {
					var dir = participant.transform.position - participant2.transform.position;
					dir.Normalize();
					var pos = (participant.transform.position + participant2.transform.position) / 2;
					pos.y = 1;
					shakePoint.transform.position = pos - dir * 0.17f;
					shakePoint2.transform.position = pos + dir * 0.17f;
					shakePoint.transform.rotation = Quaternion.LookRotation(participant2.transform.position + participant2.transform.right * -1f - participant.transform.position, Vector3.up);
					shakePoint2.transform.rotation = Quaternion.LookRotation(participant.transform.position + participant.transform.right * -1f - participant2.transform.position, Vector3.up);
				}),
				new SequenceParallel(
					participant.GetComponent<BehaviorMecanim>().Node_StartInteraction(rightHandIK, shakePoint),
					participant2.GetComponent<BehaviorMecanim>().Node_StartInteraction(rightHandIK2, shakePoint2)
				),
				new LeafWait(1000),
				new SequenceParallel(
					participant.GetComponent<BehaviorMecanim>().Node_StopInteraction(rightHandIK),
					participant2.GetComponent<BehaviorMecanim>().Node_StopInteraction(rightHandIK2)
				),
				new SequenceParallel(
					new LeafInvoke(() => { shakePoint_obj.SetActive(false); }),
					new LeafInvoke(() => { shakePoint2_obj.SetActive(false); })
				)
			)
		);
	}

	private Node SetDialogueText(String text)
    {
		return (
			new Sequence(
				new LeafInvoke(() => {
					if (DialogueUI.Available())
					{
						if (DialogueUI.Finished(text))
						{
							return RunStatus.Success;
						}
						else
						{
							DialogueUI.SetText(text);
							return RunStatus.Running;
						}
					}
					else
					{
						return RunStatus.Running;
					}
				}),
				new LeafWait(1000)
			)
		);
	}

	private Node RetrieveUserInput()
	{
		var startTime = Time.time;
		return new DecoratorInvert(
		 		new DecoratorLoop(
					new LeafInvoke(() =>
					{
						if (Input.GetKeyDown(KeyCode.Y) == true)
						{
							print("yes");
							print(currArc.ToString());
							if (currArc == StoryArc.STAY)
							{
								currArc = StoryArc.LATEESCAPE;
							}
							else
                            {
								currArc = StoryArc.ESCAPE;
							}
						}
						else if (Input.GetKeyDown(KeyCode.N) == true)
						{
							print("no");
							currArc = StoryArc.STAY;
						}
						else
						{
							return RunStatus.Running;
						}

						if (currArc != StoryArc.NONE && currArc != StoryArc.ESCAPEFAIL)
						{
							print("input: "+currArc.ToString());
							timeToDecide = Time.time - startTime;
							return RunStatus.Failure;
						}
						return RunStatus.Running;
					})
				)
			);
	}

	protected Node MaintainArcs()
	{
		return new DecoratorLoop(

			new LeafInvoke(() => {
				switch (userInput)
				{
					case -1:
						currArc = StoryArc.NONE;
						break;
					case 0:
						currArc = StoryArc.NONE;
						break;
					case 1:
						currArc = StoryArc.ESCAPE;
						break;
					case 2:
						currArc = StoryArc.STAY;
						break;
					case 3:
						currArc = StoryArc.LATEESCAPE;
						break;
					case 4:
						currArc = StoryArc.ESCAPEFAIL;
						break;
				}
			})
		);
	}


	//STORY METHODS


	protected Node BuildTreeRoot()
	{
		// return new Sequence(
		// 	ST_StoryStart(),
		// 	ST_StoryDiverge()
		// );
		return new Sequence(
			new SequenceParallel(
				JailorLetsOutPrisoner(),
				CharacterControl()
			),
			new SequenceParallel(
				LunchTime(),
				CharacterControl()
			),
			GuardCallJailor(),
			PrisonerDecides(),
			// new DecoratorForceStatus ( RunStatus.Success, ST_StayArc() ),
			new SequenceParallel(
				ST_StayArc(),
				CharacterControl()
			),
			new LeafInvoke(() =>
			{
				print("finished ST_StayArc");
			}),
			ST_EscapeArc(),
			new LeafInvoke(() =>
			{
				print("finished ST_EscapeArc, waiting indefinitely");
			}),
			new DecoratorLoop(new LeafWait(1))
		);
	}

	protected Node jailorOpensCell(bool isDoorOpen)
    {
		Val<Vector3> position = Val.V(() => cellDoorButton.transform.position);

		return (
			new Sequence(
				this.ST_Approach(jailor, cellDoorButtonWP),
				jailor.GetComponent<BehaviorMecanim>().Node_StartInteraction(rightHandIK, cellButtonIK),
                jailor.GetComponent<BehaviorMecanim>().Node_HandAnimation("POINTING", true),
				new LeafWait(1000),
				new LeafInvoke(() =>
				{
					if (!isDoorOpen)
					{
						cellDoor.transform.Translate(new Vector3(5f, 0f, 0f));
					}
					else
					{
						cellDoor.transform.Translate(new Vector3(-5f, 0f, 0f));
					}
					return RunStatus.Success;
				}),
				jailor.GetComponent<BehaviorMecanim>().Node_StopInteraction(rightHandIK),
				jailor.GetComponent<BehaviorMecanim>().Node_HandAnimation("POINTING", false)
			)
		);
    }

	protected Node CharacterControl()
	{
		Func<bool> condition = () => (nodeIsRunning && targetSelected);
		Val<Vector3> target_pose = Val.V(() => target.transform.position);
		return new Sequence(
			new DecoratorInvert(
				new DecoratorLoop(
					new BoolSelector(condition,
						// this.ST_Approach(prisoner, target_pose),
						prisoner.GetComponent<BehaviorMecanim>().Node_GoTo(target_pose),
						// new DecoratorInvert(
						new LeafInvoke(() =>
						{
							print("test");
							return RunStatus.Failure;
						})
					)
				)
			),
			new LeafInvoke(() =>
			{
				print("done with character control");
			})
		);
	}

	protected Node JailorLetsOutPrisoner()
    {
		Val<Vector3> position1 = Val.V(() => prisoner.transform.position);
		Val<Vector3> position2 = Val.V(() => cellDoorButton.transform.position);
		return new Sequence(
				new LeafInvoke ( () =>  {nodeIsRunning = true; return RunStatus.Success;}),
				jailorOpensCell(false),
				new SequenceParallel(
					new Sequence(
						new LeafWait(1500),
						jailor.GetComponent<BehaviorMecanim>().Node_OrientTowards(position1),
						new LeafWait(1500),
						// this.ST_Approach(prisoner, outsideCellWP),
						jailor.GetComponent<BehaviorMecanim>().Node_OrientTowards(position2)
						// jailorOpensCell(true)
					),
                    SetDialogueText("Jailor: Lunch time Prisoner! C'mon out!")
                ),
				new LeafInvoke ( () =>  {nodeIsRunning = false; return RunStatus.Success;})
        );
    }

	protected Node LunchTime()
    {
		Val<Vector3> position = Val.V(() => prisoner.transform.position);
		Val<Vector3> position2 = Val.V(() => apple.transform.position);
		Func<bool> condition = () => (Vector3.Distance(prisoner.transform.position, cafeteriaSeatWP.position) < 5f);

		return new Sequence(
				new LeafInvoke ( () =>  {nodeIsRunning = true; return RunStatus.Success;}),
				new SequenceParallel(
					new Sequence(
						this.ST_Approach(jailor, cafeteriaGuardWP),
						jailor.GetComponent<BehaviorMecanim>().ST_TurnToFace(position)
					),
					new Sequence(
						new LeafWait(1000), // loop until condition met
						new LeafInvoke ( () =>  {nodeIsRunning = false; print("auto control"); return RunStatus.Success;}),
						this.ST_Approach(prisoner, cafeteriaSeatWP),
						prisoner.GetComponent<BehaviorMecanim>().Node_OrientTowards(position2),
						this.ST_SitDown(prisoner)
					)
				),
				new LeafInvoke ( () =>  {nodeIsRunning = false; return RunStatus.Success;})
		);
	}

	protected Node GuardCallJailor()
	{
		Val<Vector3> faceJailor = Val.V(() => jailor.transform.position);
		Val<Vector3> faceGuard = Val.V(() => guard.transform.position);

		return
			new Sequence(
				new SequenceParallel (
					guard.GetComponent<BehaviorMecanim>().Node_OrientTowards(faceJailor),
					guard.GetComponent<BehaviorMecanim>().Node_HandAnimation("CALLOVER", true)
				),
				jailor.GetComponent<BehaviorMecanim>().Node_OrientTowards(faceGuard),
				new SequenceParallel(
					SetDialogueText("Guard: Hey there Jailor! Did you see the construction out front?"),
					jailor.GetComponent<BehaviorMecanim>().Node_HandAnimation("WAVE", true),
					this.ST_Approach(jailor, distractJailorWP)
				),
				new SequenceParallel(
					SetDialogueText("Jailor: Yeah, any inmate could just walk out... if we weren't here!"),
					this.ST_ShakeHands(jailor, guard)
				)
			);
	}

	protected Node guardJailorConverse()
    {
		return
			new Sequence(
				new RandomSelector(
					guard.GetComponent<BehaviorMecanim>().ST_PlayFaceGesture(Val.V(() => "ACKNOWLEDGE"), 500),
					guard.GetComponent<BehaviorMecanim>().ST_PlayHandGesture(Val.V(() => "THINK"), 1000),
					guard.GetComponent<BehaviorMecanim>().ST_PlayHandGesture(Val.V(() => "SURPRISED"), 1000),
					guard.GetComponent<BehaviorMecanim>().ST_PlayFaceGesture(Val.V(() => "HEADNOD"), 500)
				),
				new RandomSelector(
					jailor.GetComponent<BehaviorMecanim>().ST_PlayFaceGesture(Val.V(() => "ACKNOWLEDGE"), 500),
					jailor.GetComponent<BehaviorMecanim>().ST_PlayHandGesture(Val.V(() => "THINK"), 1000),
					jailor.GetComponent<BehaviorMecanim>().ST_PlayHandGesture(Val.V(() => "SURPRISED"), 1000),
					jailor.GetComponent<BehaviorMecanim>().ST_PlayFaceGesture(Val.V(() => "HEADNOD"), 500)
				)
		);
    }

	protected Node PrisonerDecides()
    {
		return
			new Sequence(
                prisoner.GetComponent<BehaviorMecanim>().Node_StartInteraction(rightHandIK, appleIK),
				prisoner.GetComponent<BehaviorMecanim>().Node_FaceAnimation("EAT", true),
				new SequenceParallel(
					SetDialogueText("Narrator: The guards are distracted! Quick! Decide! Should the prisoner make an escape? (Press Y or N)"),
					guardJailorConverse(),
                    RetrieveUserInput()
                ),
				new LeafInvoke(() =>
				{
					print("reached end of prisoner decides");
				})
			);
    }

	protected Node ST_EscapeArc()
	{
		// Func<bool> condition = () => (currArc == StoryArc.ESCAPE);
		Func<bool> condition = () => (currArc == StoryArc.ESCAPE || currArc == StoryArc.LATEESCAPE);
		return
			new Sequence(
				new LeafInvoke(() =>
				{
					print("starting ST_EscapeArc");
					print(currArc.ToString());
					print(StoryArc.ESCAPE.ToString());
				}),
				new BoolSelector(condition,
					new Sequence(
						new LeafInvoke(() =>
						{
							print("condition ST_EscapeArc");
						}),
						ST_EscapeArcStart(),
						ST_EscapeArcMiddle()
					),
					new LeafInvoke(() =>
					{
						print("skipped ST_EscapeArc");
					})
				),
				new LeafInvoke(() =>
				{
					print("ending ST_EscapeArc");
				})
		);
	}

	protected Node ST_EscapeArcStart()
	{
		Val<Vector3> facePrisoner = Val.V(() => prisoner.transform.position);
		return
			new Sequence(
				new LeafInvoke(() =>
				{
					print("starting ST_EscapeArcStart");
				}),
                new LeafAssert(() => currArc == StoryArc.ESCAPE),
				new LeafInvoke(() =>
				{
					print(" ST_EscapeArcStart 1");
				}),
                new LeafInvoke(() => { print("escape!"); return RunStatus.Success; }),
				new LeafInvoke(() =>
				{
					print(" ST_EscapeArcStart 2");
				}),
				SetDialogueText("You: chose to escape! Let's do it!"),
				prisoner.GetComponent<BehaviorMecanim>().Node_FaceAnimation("EAT", false),
				prisoner.GetComponent<BehaviorMecanim>().Node_StopInteraction(rightHandIK),
				new LeafInvoke(() => { apple.SetActive(false); }),
				this.ST_StandUp(prisoner),
				new LeafInvoke ( () =>  {nodeIsRunning = true; return RunStatus.Success;})
			);
	}

	protected Node ST_EscapeArcMiddle()
	{
		Val<Vector3> facePrisoner = Val.V(() => prisoner.transform.position);
		Val<Vector3> faceJailor = Val.V(() => jailor.transform.position);
		Func<bool> condition = () => (Vector3.Distance(prisoner.transform.position, outsideCellWP.position) < 5f);
		return
			new Sequence(
                new LeafAssert(() => currArc == StoryArc.ESCAPE || currArc == StoryArc.LATEESCAPE),
                // this.ST_Approach(prisoner, outsideCafeteriaWP),
				new SequenceParallel(
					SetDialogueText("Narrator: Pretend to go to the jail button and then fight the guards."),
					new Sequence(
						new LeafWait(1000),
						new LeafInvoke ( () =>  {nodeIsRunning = false; return RunStatus.Success;})
					)
				),
				new SequenceParallel(
					this.ST_Approach(jailor, fightJailorWP),
					this.ST_Approach(guard, fightGuardWP)
				),
				new SequenceParallel(
					this.ST_Approach(prisoner, outsideCellWP),
					jailor.GetComponent<BehaviorMecanim>().Node_OrientTowards(facePrisoner),
					guard.GetComponent<BehaviorMecanim>().Node_OrientTowards(facePrisoner)
				),

				new SequenceParallel(
					jailor.GetComponent<BehaviorMecanim>().Node_OrientTowards(facePrisoner),
					guard.GetComponent<BehaviorMecanim>().Node_OrientTowards(facePrisoner),
					prisoner.GetComponent<BehaviorMecanim>().Node_OrientTowards(faceJailor)
				),
				new SequenceParallel(
					SetDialogueText("Narrator: The prisoner must fight both the guards to escape!"),
					prisoner.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture(Val.V(() => "FIGHT"), 2000),
					jailor.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture(Val.V(() => "FIGHT"), 2000),
					guard.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture(Val.V(() => "FIGHT"), 2000)
				),
				new LeafInvoke(() =>
				{
					print(" RandomSelector b");
				}),
				new RandomSelector(
                    ST_EscapeArcEndFail(),
                    ST_EscapeArcEndSuccess()
				)
			);

	}

	protected Node ST_EscapeArcEndSuccess()
	{
		Val<Vector3> faceJailor = Val.V(() => jailor.transform.position);
		return
			new Sequence(
				new LeafInvoke(() =>
				{
					print(" start fight success");
				}),
                // new LeafAssert(() => currArc == StoryArc.ESCAPE || currArc == StoryArc.LATEESCAPE),
                new SequenceParallel(
					jailor.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture(Val.V(() => "DUCK"), 1500),
					new Sequence(
						prisoner.GetComponent<BehaviorMecanim>().Node_OrientTowards(faceJailor),
						prisoner.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture(Val.V(() => "FIGHT"), 1500)
					),
					guard.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture(Val.V(() => "FIGHT"), 1500)
				),
				new LeafInvoke(() =>
				{
					print(" start fight success1");
				}),
				new SequenceParallel(
					SetDialogueText("Narrator: The prisoner beat both the guards! Time to escape!"),
					guard.GetComponent<BehaviorMecanim>().Node_BodyAnimation(Val.V(() => "DUCK"), true),
					jailor.GetComponent<BehaviorMecanim>().Node_BodyAnimation(Val.V(() => "DUCK"), true)
				),
				new LeafInvoke(() =>
				{
					print(" start fight success1.1");
				}),
				// new LeafInvoke ( () =>  {nodeIsRunning = true; return RunStatus.Success;}),
				this.ST_Approach(prisoner, escapeRouteWP),
				new LeafInvoke(() =>
				{
					print(" start fight success2");
				}),
				new DecoratorLoop(
					new SequenceParallel(
						// CharacterControl(),
						prisoner.GetComponent<BehaviorMecanim>().ST_PlayHandGesture(Val.V(() => "CHEER"), 500),
						guard.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture(Val.V(() => "DUCK"), 500),
						jailor.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture(Val.V(() => "DUCK"), 500)
					)
				)
			);
	}

	protected Node ST_EscapeArcEndFail()
	{
		Val<Vector3> faceCellButton = Val.V(() => cellDoorButton.transform.position);
		return
			new Sequence(
                new LeafAssert(() => currArc == StoryArc.ESCAPE || currArc == StoryArc.LATEESCAPE),
                new SequenceParallel(
					prisoner.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture(Val.V(() => "DUCK"), 1500),
					jailor.GetComponent<BehaviorMecanim>().Node_OrientTowards(faceCellButton),
					guard.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture(Val.V(() => "FIGHT"), 1500)
				),
				SetDialogueText("Narrator: Oh no! The prisoner could not fight both the guards!"),
				prisoner.GetComponent<BehaviorMecanim>().Node_BodyAnimation(Val.V(() => "DUCK"), false),
				new LeafInvoke(() => { userInput = 4; currArc = StoryArc.ESCAPEFAIL; return RunStatus.Success; }),
				ST_StayArcEndEscapeArcFail()
			) ;

	}


	protected Node ST_StayArc()
	{
		Func<bool> condition = () => (currArc == StoryArc.STAY);
		return new Sequence(
				new LeafInvoke(() =>
				{
					print("start ST_StayArc");
				}),
				new BoolSelector(condition,
					new Sequence(
						new LeafInvoke(() =>
						{
							print("continued ST_StayArc");
						}),
						ST_StayArcStart(),
						ST_StayArcDecideAgain(),
						ST_StayArcDiverge()
					),
					new LeafInvoke(() =>
					{
						print("skipped ST_StayArc");
					})
				)
		);
	}

	protected Node ST_StayArcStart()
	{
		Val<Vector3> facePrisoner = Val.V(() => prisoner.transform.position);
		return
			new Sequence(
                new LeafAssert(() => currArc == StoryArc.STAY),
                new LeafInvoke(() => { print("StaY"); return RunStatus.Success; }),
                SetDialogueText("You: chose to stay! No hassle!"),
                new LeafWait(2000),
                jailor.GetComponent<BehaviorMecanim>().Node_OrientTowards(facePrisoner),
                guard.GetComponent<BehaviorMecanim>().Node_OrientTowards(facePrisoner),
                new SequenceParallel(
                    jailor.GetComponent<BehaviorMecanim>().ST_PlayHandGesture(Val.V(() => "CALLOVER"), 1000),
                    this.ST_Approach(jailor, cafeteriaGuardWP),
                    SetDialogueText("Jailor: Lunch is over! Get back to your cells!"),
                    new Sequence(
                        prisoner.GetComponent<BehaviorMecanim>().Node_StopInteraction(rightHandIK),
						prisoner.GetComponent<BehaviorMecanim>().Node_FaceAnimation("EAT", false),
						this.ST_StandUp(prisoner)
                    )
						// Destroy(apple);
                ),
				new LeafInvoke(() => { apple.SetActive(false); }),
				new LeafInvoke ( () =>  {nodeIsRunning = true; return RunStatus.Success;})
            );
	}

	protected Node ST_StayArcDecideAgain()
	{
		return new SequenceParallel(
			SetDialogueText("Narrator: You might still have a chance to escape! Do you want to try to escape? (Press Y or N)"),
            RetrieveUserInput()
        );
	}


	//issue node --> selector parallel does not work like i expect it to.
	protected Node ST_StayArcDiverge()
	{
		Func<bool> condition = () => (currArc == StoryArc.STAY);
		return new Sequence (
			new BoolSelector(condition,
				ST_StayArcEndEscapeArcFail(),
				ST_EscapeArcMiddle()
			),
			// ST_EscapeArcMiddle(),
			// ST_StayArcEndEscapeArcFail(),
			new LeafInvoke(() =>
			{
				print("reached end of ST_StayArcDiverge");
			})
		);
	}

	protected Node ST_StayArcEndEscapeArcFail()
	{
		Val<Vector3> facePrisoner = Val.V(() => prisoner.transform.position);
		Val<Vector3> faceJailor = Val.V(() => jailor.transform.position);
		Val<Vector3> faceGuard = Val.V(() => guard.transform.position);
		Func<bool> condition = () => (Vector3.Distance(prisoner.transform.position, outsideCellWP.position) < 5f);
		return
			new Sequence(
				new LeafInvoke(() => { print("ST_StayArcEndEscapeArcFail: " + currArc.ToString()); return RunStatus.Success; }),
				new LeafAssert(() => currArc == StoryArc.STAY || currArc == StoryArc.ESCAPEFAIL),
				new SequenceParallel(
					SetDialogueText("Narrator: Go to the jail button."),
					this.ST_Approach(jailor, cellDoorButtonWP),
					new Sequence(
						new LeafWait(1000),
						new LeafInvoke ( () =>  {nodeIsRunning = false; return RunStatus.Success;}),
						this.ST_Approach(prisoner, outsideCellWP)
					)
				),

				// this.jailorOpensCell(false),
				this.ST_Approach(prisoner, prisonerCellWP),
				this.jailorOpensCell(true),
				jailor.GetComponent<BehaviorMecanim>().Node_OrientTowards(faceGuard),
				guard.GetComponent<BehaviorMecanim>().Node_OrientTowards(faceJailor),
				new DecoratorLoop(
					guardJailorConverse()
				)
			);
	}
}
