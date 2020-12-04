using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TreeSharpPlus;
using RootMotion.FinalIK;

public class IKBT : MonoBehaviour
{
    public Transform wander1;
    public Transform wander2;
    public Transform wander3;
    public GameObject participant;

    //IK related interface
    public GameObject ball;
    public InteractionObject ikBall;
    public FullBodyBipedEffector hand;
    public FullBodyBipedEffector rightFoot;
    public InteractionObject rightFootAttractor;
    public FullBodyBipedEffector leftFoot;
    public InteractionObject leftFootAttractor;

    private BehaviorAgent behaviorAgent;
    // Use this for initialization
    void Start()
    {
        behaviorAgent = new BehaviorAgent(this.BuildTreeRoot());
        BehaviorManager.Instance.Register(behaviorAgent);
        behaviorAgent.StartBehavior();
    }

    #region IK related function

    protected Node PickUp(GameObject p)
    {
        return new Sequence(this.Node_BallStop(),
                            p.GetComponent<BehaviorMecanim>().Node_StartInteraction(hand, ikBall),
                            new LeafWait(1000),
                            p.GetComponent<BehaviorMecanim>().Node_StopInteraction(hand));
    }

    public Node Node_BallStop()
    {
        return new LeafInvoke(() => {
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;

            return RunStatus.Success;
        });
    }

    public Node PutDown(GameObject p)
    {
        return new Sequence(p.GetComponent<BehaviorMecanim>().Node_StartInteraction(hand, ikBall),
                            new LeafWait(300),
                            this.Node_BallMotion(),
                            new LeafWait(500), p.GetComponent<BehaviorMecanim>().Node_StopInteraction(hand));
    }

    public Node Node_BallMotion()
    {
        return new LeafInvoke(() => {
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            rb.velocity = Vector3.zero;
            rb.isKinematic = false;
            ball.transform.parent = null;
            return RunStatus.Success;
        });
    }
    
    public Node FootTap(GameObject p, FullBodyBipedEffector foot, InteractionObject attractor)
    {
        return new Sequence(
                p.GetComponent<BehaviorMecanim>().Node_StartInteraction(foot, attractor),
                new LeafWait(200),
                p.GetComponent<BehaviorMecanim>().Node_StopInteraction(foot)
            );
    }

    public Node ChaChaRealSmooth(GameObject p, int iterations = 1)
    {
        int counter = 0;

        return new DecoratorInvert(
                new Sequence(
                    new LeafInvoke(() => counter = 0),
                    new DecoratorLoop(
                        new Sequence(
                            new LeafAssert(() => counter < iterations),

                            FootTap(p, leftFoot, leftFootAttractor),
                            FootTap(p, rightFoot, rightFootAttractor),

                            new LeafInvoke(() => counter++)
                        )
                    )
                )
            );
    }

    #endregion

    protected Node ST_ApproachAndWait(Transform target)
    {
        Val<Vector3> position = Val.V(() => target.position);
        return new Sequence(participant.GetComponent<BehaviorMecanim>().Node_GoTo(position), new LeafWait(1000));
    }

    protected Node BuildTreeRoot()
    {
        Node roaming = new DecoratorLoop(
                new Selector(
                    new Sequence(
                        this.ST_ApproachAndWait(this.wander1),
                        new DecoratorLoop(
                            //new Sequence(this.PickUp(participant), ChaChaRealSmooth(participant, 3), this.PutDown(participant)))
                            new Sequence(
                                participant.GetComponent<BehaviorMecanim>().ST_TurnToFace(Val.V(() => ball.transform.position)),
                                this.PickUp(participant), 
                                this.PutDown(participant)
                            )
                        )
                    ),
                    this.ST_ApproachAndWait(this.wander2)
                )
            );
        return roaming;
    }
}
