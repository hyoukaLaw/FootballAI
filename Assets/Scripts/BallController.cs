using UnityEngine;

public class BallController : MonoBehaviour
{
    private Vector3 _targetPos;
    private bool _isMoving = false;
    private float _speed = 15.0f;

// (可选优化) 防止踢球瞬间自己立刻把球停住
    private float _flyTimer = 0f;

    public void KickTo(Vector3 target, float speed)
    {
        _targetPos = target;
        _speed = speed;
        _isMoving = true;
        _flyTimer = 0.2f; // 给0.2秒的"飞行保护期"
    }

    void Update()
    {
        if (!_isMoving) return;

        if (_flyTimer > 0)
        {
            _flyTimer -= Time.deltaTime;
            // 保护期内只移动，不检测拦截
            transform.position = Vector3.MoveTowards(transform.position, _targetPos, _speed * Time.deltaTime);
            return; 
        }

        // 保护期过后，才允许被拦截
        if (MatchManager.Instance.CurrentBallHolder != null)
        {
            _isMoving = false;
            return;
        }

        // --- 正常的飞行逻辑 ---

        // 2. 移动
        transform.position = Vector3.MoveTowards(transform.position, _targetPos, _speed * Time.deltaTime);

        // 3. 到达检测 (自然滚动停止)
        // 如果一直没人接，球滚到了终点也该停了
        if (Vector3.Distance(transform.position, _targetPos) < 0.1f)
        {
            _isMoving = false;
        }
    }
}