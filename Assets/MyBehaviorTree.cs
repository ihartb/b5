using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using TreeSharpPlus;
using RootMotion.FinalIK;

public class MyBehaviorTree : MonoBehaviour
{
	//agents 
	public GameObject jailor;
	public GameObject prisoner;
	public GameObject wanderPoints;

	//wander points 
	public Transform cellDoorButtonWP;
	public Transform outsideCellWP;
	public Transform cafeteriaGuardWP;
	public Transform cafeteriaFoodWP;
	public Transform cafeteriaSeatWP;
	public Transform cafeteriaTable;

	public Transform escapeRouteWP;

	//objects
	public GameObject foodPlate;
	public GameObject buffetLine;
	public GameObject cellDoor;
	public GameObject cellDoorButton;

	//IK
	public InteractionObject cellButtonIK;
	public InteractionObject foodPlateIK;

	public FullBodyBipedEffector rightHandIK;
	public FullBodyBipedEffector leftHandIK;


	private BehaviorAgent behaviorAgent;
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
	void Update ()
	{
	}

	protected Node ST_Approach(GameObject participant, Transform target)
	{
		Val<Vector3> position = Val.V (() => target.position);
		return participant.GetComponent<BehaviorMecanim>().Node_GoTo(position);

	}

	protected Node SitDown(GameObject participant)
	{
		return (
			new LeafInvoke(() =>
			{
				return prisoner.GetComponent<BehaviorMecanim>().Character.SitDown();
			})
		);
	}

	protected Node jailorApproachInteractCellDoor(bool isOpen)
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
					if (isOpen)
					{
						cellDoor.transform.Translate(new Vector3(5f, 0f, 0f));
					}
					else{
						cellDoor.transform.Translate(new Vector3(-5f, 0f, 0f));
					}
					return RunStatus.Success;
				}),
				jailor.GetComponent<BehaviorMecanim>().Node_StopInteraction(rightHandIK),
				jailor.GetComponent<BehaviorMecanim>().Node_HandAnimation("POINTING", false)
			)
		);
    }

	protected Node prisonerLeavesCell()
    {
		Val<Vector3> position1 = Val.V(() => prisoner.transform.position);
		Val<Vector3> position2 = Val.V(() => cellDoorButton.transform.position);

		return (
			new Sequence(
				new SequenceParallel(
					jailor.GetComponent<BehaviorMecanim>().Node_OrientTowards(position1),
					this.ST_Approach(prisoner, outsideCellWP)
				),
				jailor.GetComponent<BehaviorMecanim>().Node_OrientTowards(position2)
			)
		) ;
    }

	protected Node prisonerEats()
	{
		Val<Vector3> position1 = Val.V(() => buffetLine.transform.position);
		Val<Vector3> position2 = Val.V(() => cafeteriaTable.transform.position);

		return (
			new Sequence(
				this.ST_Approach(prisoner, cafeteriaFoodWP),
				jailor.GetComponent<BehaviorMecanim>().Node_OrientTowards(position1),
				prisoner.GetComponent<BehaviorMecanim>().Node_StartInteraction(rightHandIK, foodPlateIK),
				this.ST_Approach(prisoner, cafeteriaSeatWP),
				jailor.GetComponent<BehaviorMecanim>().Node_OrientTowards(position2),
				this.SitDown(prisoner),
				prisoner.GetComponent<BehaviorMecanim>().Node_StopInteraction(rightHandIK),
				new LeafWait(2000)
			)
		);
	}

	protected Node LunchTime()
    {
		Val<Vector3> position = Val.V(() => prisoner.transform.position);
		return (
			new SequenceParallel(
				new Sequence(
						this.ST_Approach(jailor, cafeteriaGuardWP),
						jailor.GetComponent<BehaviorMecanim>().Node_OrientTowards(position),
						jailor.GetComponent<BehaviorMecanim>().Node_HeadLook(position)
				),
				new Sequence(
					prisonerEats()
				)
			)
		);
	}

	//TODO
	protected Node EscapeTime()
	{
		///b5/Assets/Core/Scripts/Character/BodyMecanim.cs line 369 : fight animation ? 
		return null;
	}

	protected Node BuildTreeRoot()
	{
		Node roaming =
			new DecoratorLoop(
				new Sequence(
                    jailorApproachInteractCellDoor(true),
                    new LeafWait(1500),
					prisonerLeavesCell(),
					jailorApproachInteractCellDoor(false),
                    LunchTime()
				)
			) ;
		return roaming;
	}
}
