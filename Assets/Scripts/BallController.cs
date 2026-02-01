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
        MyLog.LogInfo($"{_lastKicker.name} Ball KickTo {_targetPos} {_speed}");
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
        _ballGameObject.transform.position = Vector3.MoveTowards(_ballGameObject.transform.position, _targetPos, _speed * Time.deltaTime);
        if (Vector3.Distance(_ballGameObject.transform.position, _targetPos) < FootballConstants.SamePositionDistance)// 3. 到达检测 (自然滚动停止)
        {
            _isMoving = false;
        }
    }
    
    public GameObject GetLastKicker()
    {
        if(_lastKickerDuration > 0f)
            return _lastKicker;
        return null;
    }
    
    public void UpdateLastKickerReset()
    {
        _lastKickerTimer -= Time.deltaTime;
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