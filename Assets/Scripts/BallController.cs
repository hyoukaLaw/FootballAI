using UnityEngine;

public class BallController : MonoBehaviour
{
    private Vector3 _targetPos;
    private bool _isMoving = false; // 当前是否被踢出在运动
    private float _speed = 15.0f;
    private GameObject _lastKicker;

    public void KickTo(GameObject kicker, Vector3 target, float speed)
    {
        _targetPos = target;
        _speed = speed;
        _isMoving = true;
        _lastKicker = kicker;
        Debug.Log($"{_lastKicker.name} Ball KickTo {_targetPos} {_speed}");
    }

    void Update()
    {
        if (!_isMoving) return;
        
        if (MatchManager.Instance.Context.BallHolder != null && 
            MatchManager.Instance.Context.BallHolder != _lastKicker)
        {
            _isMoving = false;
            return;
        }
        // --- 正常的飞行逻辑 ---
        // 2. 移动
        transform.position = Vector3.MoveTowards(transform.position, _targetPos, _speed * Time.deltaTime);
        Debug.Log($"Ball Move To {_targetPos} {_speed}");
        // 3. 到达检测 (自然滚动停止)
        // 如果一直没人接，球滚到了终点也该停了
        if (Vector3.Distance(transform.position, _targetPos) < FootballConstants.SamePositionDistance)
        {
            _isMoving = false;
        }
    }
    

    public GameObject GetLastKicker()
    {
        return _lastKicker;
    }

    public void ForceStopMove()
    {
        _isMoving = false;
    }
}