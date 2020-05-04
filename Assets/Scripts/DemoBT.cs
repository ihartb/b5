using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TreeSharpPlus;
using UnityEngine;

public class DemoBT : MonoBehaviour
{

    private BehaviorAgent behaviorAgent;

    private enum StoryArc
    {
        MENU = 0, 
        ADD,
        FREEFORM
    }

    private StoryArc currArc = StoryArc.MENU;
    private int userInput = 0;

    void Start()
    {
        behaviorAgent = new BehaviorAgent(this.BuildTreeRoot());
        BehaviorManager.Instance.Register(behaviorAgent);
        behaviorAgent.StartBehavior();
    }
    
    private Node BuildTreeRoot()
    {
        return new DecoratorLoop(
                new Sequence(
                    new SelectorParallel(
                        MenuArc(),
                        AddArc(),
                        FreeformArc()
                    )
                )
            );
    }

    #region Arc Assertions

    private Node CheckMenuArc()
    {
        return new Sequence(
                new LeafAssert(() => (StoryArc)userInput == StoryArc.MENU),
                new LeafInvoke(() => currArc = StoryArc.MENU)
            );
    }

    private Node CheckAddArc()
    {
        return new Sequence(
                new LeafAssert(() => (StoryArc)userInput == StoryArc.ADD),
                new LeafInvoke(() => currArc = StoryArc.ADD)
            );
    }

    private Node CheckFreeformArc()
    {
        return new Sequence(
                new LeafAssert(() => (StoryArc)userInput == StoryArc.FREEFORM),
                new LeafInvoke(() => currArc = StoryArc.FREEFORM)
            );
    }

    #endregion

    #region Arcs
    
    private Node MenuArc()
    {
        return new Sequence(
                CheckMenuArc(),
                new LeafInvoke(() => print("Please choose Add (1) or Freeform (2)")),
                RetrieveUserInput()
            );
    }

    private Node AddArc()
    {
        var nums = Enumerable.Range(0, 20).Select(i => new Number(i)).ToArray();

        return new Sequence(
                CheckAddArc(),
                //new LeafInvoke(() => print(currArc)),

                //Add(nums[1], nums[2]),
                //Add3(new[] { nums[1], nums[2], nums[3] }),
                MultiAdd(new[] { nums[1], nums[2], nums[3], nums[4], nums[5], nums[6] }),
                PrintNumber(nums[1]),

                new LeafInvoke(() => userInput = (int)StoryArc.MENU)
            );
    }

    private Node FreeformArc()
    {
        var nums = Enumerable.Range(0, 20).Select(i => new Number(i)).ToArray();

        return new Sequence(
                CheckFreeformArc(),
                //new LeafInvoke(() => print(currArc)),

                Multiply(nums[11], nums[10]),
                PrintNumber(nums[11]),
                
                new LeafInvoke(() => userInput = (int)StoryArc.MENU)
            );
    }

    #endregion

    #region BTs
    
    private Node Add3(Number[] nums)
    {
        return new Selector(
                new LeafAssert(() => nums.Length != 3),
                new Sequence(
                    Add(Val.V(() => nums[0]), Val.V(() => nums[1])),
                    Add(Val.V(() => nums[0]), Val.V(() => nums[2]))
                )
            );
    }

    private Node MultiAdd(Number[] nums)
    {
        int index = 0;
        var seq = new Sequence(
                new LeafInvoke(() => index = 1)
            );
        for (int i = 1; i < nums.Length; i++)
        {
            var otherNum = nums[i];

            seq.Children.Add(Add(Val.V(() => nums[0]), Val.V(() => nums[index++])));
        }
        
        return seq;
    }

    #endregion

    #region Affordances

    private Node RetrieveUserInput()
    {
        return new DecoratorInvert(
                new DecoratorLoop(
                    new Sequence(
                        new LeafInvoke(
                            () => {
                                var input = -1;

                                if (Input.GetKey("0"))
                                    input = 0;
                                if (Input.GetKey("1"))
                                    input = 1;
                                if (Input.GetKey("2"))
                                    input = 2;

                                if (input >= 0 && input < 3)
                                {
                                    userInput = input;

                                    return RunStatus.Failure;
                                }
                                else
                                {
                                    return RunStatus.Running;
                                }
                            }
                        ),
                        new LeafInvoke(() => print("Waiting..."))
                    )
                )
            );
    }

    private Node Add(Val<Number> a, Val<Number> b)
    {
        return new LeafInvoke(() => {
            a.Value.val += b.Value.val;
        });
    }

    private Node Multiply(Val<Number> a, Val<Number> b)
    {
        return new LeafInvoke(() => {
            a.Value.val *= b.Value.val;
        });
    }

    private Node PrintNumber(Number a)
    {
        return new LeafInvoke(() => print("Value: " + a.val));
    }

    #endregion

    #region Control Nodes

    private class NewControlNode : NodeGroup
    {

        public NewControlNode(params Node[] children)
            : base(children)
        {
        }

        public override IEnumerable<RunStatus> Execute()
        {
            foreach (Node node in this.Children)
            {
                this.Selection = node;
                node.Start();

                // If the current node is still running, report that. Don't 'break' the enumerator
                RunStatus result;
                while ((result = this.TickNode(node)) == RunStatus.Running)
                    yield return RunStatus.Running;

                // Call Stop to allow the node to clean anything up.
                node.Stop();

                // Clear the selection
                this.Selection.ClearLastStatus();
                this.Selection = null;

                if (result == RunStatus.Failure)
                {
                    yield return RunStatus.Failure;
                    yield break;
                }

                yield return RunStatus.Running;
            }
            yield return RunStatus.Success;
            yield break;
        }
        
    }

    #endregion

    #region Utility Classes

    private class Number
    {
        public int val;

        public Number(int v)
        {
            val = v;
        }
    }

    #endregion
}
