@echo off
:: 1. 设置代理 (注意: HTTPS_PROXY 的值通常也是 http:// 开头)
set HTTP_PROXY=http://127.0.0.1:1080
set HTTPS_PROXY=http://127.0.0.1:1080

:: 2. 设置不走代理的地址 (本地地址豁免)
set NO_PROXY=localhost,127.0.0.1,::1,0.0.0.0

echo =================================================
echo [代理环境已加载]
echo HTTP/HTTPS Proxy: 127.0.0.1:1080
echo NO_PROXY: %NO_PROXY%
echo 正在启动 opencode...
echo =================================================

:: 3. 运行 opencode
:: 这里的 %* 表示如果你给这个脚本传了参数，它会自动传给 opencode
opencode %*

:: 4. 保持窗口打开
:: 这样你可以看到 opencode 的输出日志，或者在 opencode 结束后继续使用这个带代理环境的 CMD
cmd /k