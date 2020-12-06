using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using System.Collections;
using TreeSharpPlus;

namespace TreeSharpPlus
{
    /// <summary>
    /// Evaluates a lambda function. Returns RunStatus.Success if the lambda
    /// evaluates to true. Returns RunStatus.Failure if it evaluates to false.
    /// </summary>
    public class RandomSelector : NodeGroup
    {

        public RandomSelector(params Node[] children)
            : base(children)
        {
        }

        public override IEnumerable<RunStatus> Execute()
        {
            int i = UnityEngine.Random.Range(0, this.Children.Count);
			Node node = Children[1];
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

            yield return RunStatus.Success;
            yield break;
        }
    }
}
