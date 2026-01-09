using UnityEngine;

public class BallController : MonoBehaviour
{
    private Vector3 _targetPos;
    private bool _isMoving = false;
    private float _speed = 15.0f;

    // 供 AI 调用的接口：踢我！
    public void KickTo(Vector3 target, float speed)
    {
        _targetPos = target;
        _speed = speed;
        _isMoving = true;
    }

    void Update()
    {
        if (_isMoving)
        {
            // 球自己飞
            transform.position = Vector3.MoveTowards(transform.position, _targetPos, _speed * Time.deltaTime);

            // 到达检测
            if (Vector3.Distance(transform.position, _targetPos) < 0.1f)
            {
                _isMoving = false;
                // 这里球停下了，等待 MatchManager 下一帧判定球权归属给接球人
            }
        }
    }
}