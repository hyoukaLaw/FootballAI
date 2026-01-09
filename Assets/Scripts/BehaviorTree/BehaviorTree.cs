namespace BehaviorTree
{
    public class BehaviorTree
    {
        // 根节点
        private Node _rootNode;
        // 该树关联的黑板
        private FootballBlackboard _blackboard;

        public BehaviorTree(FootballBlackboard blackboard)
        {
            _blackboard = blackboard;
        }

        // 设置根节点
        public void SetRoot(Node root)
        {
            _rootNode = root;
        }

        // 每帧由外部控制器调用
        public void Tick()
        {
            if (_rootNode != null)
            {
                _rootNode.Evaluate();
            }
        }

        public FootballBlackboard GetBlackboard() => _blackboard;
    }
}