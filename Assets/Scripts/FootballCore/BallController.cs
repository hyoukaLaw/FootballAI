using UnityEngine;

public class BallController
{
    private Vector3 _targetPos;
    private bool _isMoving = false; // 当前是否被踢出在运动
    private float _speed = 15.0f;
    private GameObject _lastKicker;
    private GameObject _ballGameObject;
    private float _lastKickerTimer = 0f;
    private float _lastKickerDuration = 0.5f;
    
    
    public BallController(GameObject ballGameObject)
    {
        _ballGameObject = ballGameObject;
    }

    public void KickTo(GameObject kicker, Vector3 target, float speed)
    {
        _targetPos = target;
        _speed = speed;
        _isMoving = true;
        _lastKicker = kicker;
        _lastKickerTimer = _lastKickerDuration;
        //MyLog.LogInfo($"{_lastKicker.name} Ball KickTo {_targetPos} {_speed}");
    }

    public void Update()
    {
        UpdateLastKickerReset();
        if (!_isMoving)
        {
            var ballHolder = MatchManager.Instance.Context.GetBallHolder();
            if(ballHolder != null)
                _ballGameObject.transform.position = ballHolder.transform.position;
            return;
        }
        
        if (MatchManager.Instance.Context.GetBallHolder() != null && 
            MatchManager.Instance.Context.GetBallHolder() != _lastKicker)
        {
            _isMoving = false;
            return;
        }
        // --- 正常的飞行逻辑 ---
        _ballGameObject.transform.position = Vector3.MoveTowards(_ballGameObject.transform.position, _targetPos, _speed * TimeManager.Instance.GetDeltaTime());
        if (Vector3.Distance(_ballGameObject.transform.position, _targetPos) < FootballConstants.SamePositionDistance)// 3. 到达检测 (自然滚动停止)
        {
            _isMoving = false;
        }
    }
    
    /// <summary>
    /// 返回历史上最后一次触球的球员。
    /// 该值不会因为保护计时超时而清空，用于出界/判罚等需要追溯最后触球人的场景。
    /// </summary>
    public GameObject GetLastKicker()
    {
        return _lastKicker;
    }

    /// <summary>
    /// 返回处于保护窗口内的最后触球球员。
    /// 仅当最近触球计时器仍大于0时返回，用于短时间内避免立即重新判定为持球人。
    /// </summary>
    public GameObject GetRecentKicker()
    {
        if (_lastKickerTimer > 0f)
            return _lastKicker;
        return null;
    }
    
    public void UpdateLastKickerReset()
    {
        if (_lastKickerTimer <= 0f)
            return;

        _lastKickerTimer -= TimeManager.Instance.GetDeltaTime();
        if (_lastKickerTimer <= 0f)
        {
            _lastKickerTimer = 0f;
        }
    }

    public bool GetIsMoving()
    {
        return _isMoving;
    }
}
