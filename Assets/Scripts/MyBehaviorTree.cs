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

	//objects
	public GameObject apple;
	public GameObject cellDoor;
	public GameObject cellDoorButton;
	public Transform cafeteriaTable;

	//IK
	public InteractionObject cellButtonIK;
	public InteractionObject appleIK;
	public InteractionObject shakePoint;
	public InteractionObject shakePoint2;
	public FullBodyBipedEffector rightHandIK;
	public FullBodyBipedEffector rightHandIK2;

	//UI
	public Text dialogue;

	private BehaviorAgent behaviorAgent;
	private int userInput = 0;
	private StoryArc currArc = StoryArc.NONE;
	private float timeToDecide = 0;

	private enum StoryArc
	{
		NONE = 0,
		ESACPE,
		STAY,
	}

	// -2 not there yet in story, -1 waiting for reply, 0-n answer
	private int midStoryWait = -2;

	// Use this for initialization
	void Start ()
	{
		behaviorAgent = new BehaviorAgent (this.BuildTreeRoot ());
		BehaviorManager.Instance.Register (behaviorAgent);
		behaviorAgent.StartBehavior ();
		//this doesnt work
		//jailor = jailorObj.GetComponent<BehaviorMecanim>();
		//prisoner = prisonerObj.GetComponent<BehaviorMecanim>();

	}

	// Update is called once per frame
	// Update is called once per frame
	void Update()
	{
	}

	//control node
	private Node RandomSelector(params Node[] children)
	{
		int i = UnityEngine.Random.Range(0, children.Length);
		return children[i];
	}

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
				)
			)
		);
	}

	//doesnt work here.... 
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
					new SequenceParallel(
						new LeafInvoke(() =>
						{
							var input = 0;
							if (Input.GetKeyDown(KeyCode.Y) == true)
							{
								print("yes");
								input = 1;
							}
							if (Input.GetKeyDown(KeyCode.N) == true)
							{
								print("no");
								input = 2;
							}
						
							if (input > 0 && input < 3)
							{
								userInput = input;
								currArc = (StoryArc)input;
								timeToDecide = Time.time - startTime;
								return RunStatus.Failure;
							}
							else
							{
								return RunStatus.Running;
							}
						}),
						gaurdJailorConverse()
					)
				)
			);
	}

	private Node CheckEscapeArc()
	{
		return new Sequence(
				new LeafAssert(() => (StoryArc)userInput == StoryArc.ESACPE),
				new LeafInvoke(() => currArc = StoryArc.ESACPE)
			);
	}

	private Node CheckStayArc()
	{

		return new Sequence(
				new LeafAssert(() => (StoryArc)userInput == StoryArc.STAY),
				new LeafInvoke(() => currArc = StoryArc.STAY)
			);
	}

	//STORY METHODS
	//beginning story
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

	protected Node JailorLetsOutPrisoner()
    {
		Val<Vector3> position1 = Val.V(() => prisoner.transform.position);
		Val<Vector3> position2 = Val.V(() => cellDoorButton.transform.position);

		return (
			new Sequence(
				jailorOpensCell(false),
				new SequenceParallel(
					new Sequence(
						new LeafWait(1500),
						jailor.GetComponent<BehaviorMecanim>().Node_OrientTowards(position1),
						this.ST_Approach(prisoner, outsideCellWP),
						jailor.GetComponent<BehaviorMecanim>().Node_OrientTowards(position2),
						jailorOpensCell(true)
					)
                    ,
                    SetDialogueText("Jailor: Lunch time Prisoner! C'mon out!")
                )
            )
		) ;
    }

	protected Node LunchTime()
    {
		Val<Vector3> position = Val.V(() => prisoner.transform.position);
		Val<Vector3> position2 = Val.V(() => apple.transform.position);

		return (
			new SequenceParallel(
				new Sequence(
					this.ST_Approach(jailor, cafeteriaGuardWP),
					jailor.GetComponent<BehaviorMecanim>().ST_TurnToFace(position)
				),
				new Sequence(
					this.ST_Approach(prisoner, cafeteriaSeatWP),
					prisoner.GetComponent<BehaviorMecanim>().Node_OrientTowards(position2),
					this.ST_SitDown(prisoner)
				)
			)
		);
	}

	//middle story
	protected Node guardCallJailor()
	{
		Val<Vector3> faceJailor = Val.V(() => jailor.transform.position);
		Val<Vector3> faceGuard = Val.V(() => guard.transform.position);

		return
			new Sequence(
				new SequenceParallel (
					guard.GetComponent<BehaviorMecanim>().Node_OrientTowards(faceJailor),
					guard.GetComponent<BehaviorMecanim>().Node_HandAnimation("CALLOVER", true),
					SetDialogueText("Guard: Hey there Jailor! Didn't see you during your shift yesterday!")
				),
				jailor.GetComponent<BehaviorMecanim>().Node_OrientTowards(faceGuard),
				new SequenceParallel(
					jailor.GetComponent<BehaviorMecanim>().Node_HandAnimation("WAVE", true),
					this.ST_Approach(jailor, distractJailorWP)
				),
				this.ST_ShakeHands(jailor, guard)
			);
	}

	protected Node gaurdJailorConverse()
    {
		return
			new Sequence(
				RandomSelector(
					guard.GetComponent<BehaviorMecanim>().ST_PlayFaceGesture(Val.V(() => "ACKNOWLEDGE"), 500),
					guard.GetComponent<BehaviorMecanim>().ST_PlayHandGesture(Val.V(() => "THINK"), 1000),
					guard.GetComponent<BehaviorMecanim>().ST_PlayHandGesture(Val.V(() => "SURPRISED"), 1000),
					guard.GetComponent<BehaviorMecanim>().ST_PlayFaceGesture(Val.V(() => "HEADNOD"), 500)
				),
				RandomSelector(
					jailor.GetComponent<BehaviorMecanim>().ST_PlayFaceGesture(Val.V(() => "ACKNOWLEDGE"), 500),
					jailor.GetComponent<BehaviorMecanim>().ST_PlayHandGesture(Val.V(() => "THINK"), 1000),
					jailor.GetComponent<BehaviorMecanim>().ST_PlayHandGesture(Val.V(() => "SURPRISED"), 1000),
					jailor.GetComponent<BehaviorMecanim>().ST_PlayFaceGesture(Val.V(() => "HEADNOD"), 500)
				)
		);
    }

	protected Node prisonerDecides()
    {
		return 
			new Sequence(
                prisoner.GetComponent<BehaviorMecanim>().Node_StartInteraction(rightHandIK, appleIK),
				prisoner.GetComponent<BehaviorMecanim>().Node_FaceAnimation("EAT", true),
				new SequenceParallel(
					SetDialogueText("Narrator: The guards are distracted! Quick! Decide! Should the prisoner make an escape? (Press Y or N)"),
					RetrieveUserInput()
				)
			);
    }

	protected Node EscapeArc()
    {
		Val<Vector3> facePrisoner = Val.V(() => prisoner.transform.position);
		return
			new Sequence(
				CheckEscapeArc(),
				SetDialogueText("You: chose to escape! Let's do it!"),
				prisoner.GetComponent<BehaviorMecanim>().Node_StopInteraction(rightHandIK)
				//TODO
				//add more things to do for escape...

				//this.ST_StandUp(prisoner),
				//this.ST_Approach(prisoner, cafeteriaGuardWP),
				//new SequenceParallel(
				//	this.ST_Approach(prisoner, outsideCellWP),
				//	new Sequence(
				//		new SequenceParallel(
				//			jailor.GetComponent<BehaviorMecanim>().Node_OrientTowards(facePrisoner),
				//			guard.GetComponent<BehaviorMecanim>().Node_OrientTowards(facePrisoner)
				//		),
				//		new SequenceParallel(
				//			this.ST_Approach(jailor, outsideCellWP),
				//			this.ST_Approach(jailor, outsideCellWP)
				//		)
				//	)
				//),
				//new SequenceParallel(
				//	prisoner.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture(Val.V(() => "FIGHT"), 3000),
				//	jailor.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture(Val.V(() => "FIGHT"), 3000),
				//	guard.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture(Val.V(() => "FIGHT"), 3000)
				//)
			);
	}

	protected Node StayArc()
	{
		Val<Vector3> facePrisoner = Val.V(() => prisoner.transform.position);
		return
			new Sequence(
				CheckStayArc(),
				SetDialogueText("You: chose to stay! No hassle!"),
				jailor.GetComponent<BehaviorMecanim>().Node_OrientTowards(facePrisoner),
				guard.GetComponent<BehaviorMecanim>().Node_OrientTowards(facePrisoner),
				new SequenceParallel(
					jailor.GetComponent<BehaviorMecanim>().ST_PlayHandGesture(Val.V(() => "CALLOVER"), 1000),
					this.ST_Approach(jailor, cafeteriaGuardWP),
					SetDialogueText("Jailor: Lunch is over! Get back to your cells!"),
					new Sequence(
						prisoner.GetComponent<BehaviorMecanim>().Node_StopInteraction(rightHandIK),
						this.ST_StandUp(prisoner)
					)
				)
			); 
	}


	protected Node ST_StoryMiddle()
	{

		return 
			new Sequence(
                guardCallJailor(),
                prisonerDecides(),
				new LeafWait(1000),
				//next line is issue line, doesnt work with anything
				//not sure which node to use here... selector/selector parallel/race gives errors..check demobt that one seems to work tho
				//only work for the option set first...
				new SelectorParallel( 
					StayArc(),
					EscapeArc()
				)

				//new DecoratorLoop ( //alternative to above....doesnt work either tho???
				//	new DecoratorInvert(
				//		new SelectorParallel( //not sure which node to use here... selector/selector parallel/race gives errors
				//			StayArc(),
				//			EscapeArc()
				//		)
				//	)

				//)
			);
	}

	protected Node ST_StoryStart()
	{
		return new Sequence(
            JailorLetsOutPrisoner(),
            LunchTime()
		);
	}

	protected Node ST_StoryEnd()
	{
		//escape arc..
			//not sure
		//stay arc
			//take back to cell from current location
		return null;
	}

	protected Node BuildTreeRoot()
	{
		return new Sequence(
			ST_StoryStart(),
			ST_StoryMiddle()
		);
	}
}
