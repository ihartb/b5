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
    public class BoolSelector : NodeGroup
    {
        protected Func<bool> condition = null;
        protected Node child1 = null;
        protected Node child2 = null;

        public BoolSelector(Func<bool> _condition, Node c1, Node c2)
        {
            this.condition = _condition;
			// child1 = children[0];
			// child2 = children[1];
			child1 = c1;
			child2 = c2;
        }

        public override IEnumerable<RunStatus> Execute()
        {
			Node node;
			if (this.condition()) node = child1;
			else node = child2;
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
