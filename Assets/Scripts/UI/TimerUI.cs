using UnityEngine;
using TMPro;
using ProjectChicken.Core;

namespace ProjectChicken.UI
{
    /// <summary>
    /// 倒计时UI：显示游戏剩余时间
    /// </summary>
    public class TimerUI : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private TMP_Text timerText; // 倒计时文本（TextMeshPro）

        [Header("颜色配置")]
        [SerializeField] private Color normalColor = Color.white; // 正常颜色（白色）
        [SerializeField] private Color warningColor = Color.red; // 警告颜色（红色，时间少于10秒时）
        [SerializeField] private float warningThreshold = 10f; // 警告阈值（秒）

        private void OnEnable()
        {
            // 订阅倒计时更新事件
            GameManager.OnTimerUpdated += OnTimerUpdated;
        }

        private void OnDisable()
        {
            // 取消订阅：防止内存泄漏
            GameManager.OnTimerUpdated -= OnTimerUpdated;
        }

        /// <summary>
        /// 倒计时更新事件处理
        /// </summary>
        /// <param name="remainingTime">剩余时间（秒）</param>
        private void OnTimerUpdated(float remainingTime)
        {
            if (timerText == null)
            {
                return;
            }

            // 格式化时间为 MM:SS 格式
            string formattedTime = FormatTime(remainingTime);
            timerText.text = formattedTime;

            // 根据剩余时间设置颜色
            if (remainingTime <= warningThreshold)
            {
                // 时间少于阈值，显示红色
                timerText.color = warningColor;
            }
            else
            {
                // 正常时间，显示白色
                timerText.color = normalColor;
            }
        }

        /// <summary>
        /// 格式化时间为 MM:SS 格式
        /// </summary>
        /// <param name="timeInSeconds">时间（秒）</param>
        /// <returns>格式化后的时间字符串（MM:SS）</returns>
        private string FormatTime(float timeInSeconds)
        {
            // 确保时间不为负数
            timeInSeconds = Mathf.Max(0f, timeInSeconds);

            // 计算分钟和秒
            int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60f);

            // 格式化为 MM:SS（补零）
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}

