using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

/// <summary>
/// FeatBit Documentation Resources
/// Provides FeatBit-related documentation, guides, and best practices
/// </summary>
[McpServerResourceType]
public class FeatBitResources(ILogger<FeatBitResources> logger)
{
    [McpServerResource]
    [Description("Get complete FeatBit SDK list and integration guide, including usage instructions and best practices for all languages")]
    public string GetSdkDocumentation(
        [Description("Resource URI, format: featbit://docs/{category}, valid values: sdks, quickstart, best-practices")]
        string uri = "featbit://docs/sdks")
    {
        logger.LogInformation("MCP Resource Called: GetSdkDocumentation with uri={Uri}", uri);
        
        var category = uri.Replace("featbit://docs/", "").ToLower();
        
        return category switch
        {
            "sdks" => GetSdkListDocumentation(),
            "quickstart" => GetQuickstartGuide(),
            "best-practices" => GetBestPractices(),
            _ => GetDefaultDocumentation()
        };
    }

    private static string GetSdkListDocumentation()
    {
        return """
        # FeatBit SDK 完整列表
        
        FeatBit 提供跨平台的 SDK 支持，涵盖客户端和服务端场景。
        
        ## 📱 客户端 SDK
        
        客户端 SDK 适用于移动应用、单页应用（SPA）等需要实时更新的场景。
        
        ### JavaScript/TypeScript
        - **包名**: `featbit-js-client-sdk`
        - **安装**: `npm install featbit-js-client-sdk`
        - **文档**: https://docs.featbit.co/sdk/client-side-sdks/javascript
        - **仓库**: https://github.com/featbit/featbit-js-client-sdk
        - **特点**: 支持浏览器环境，实时更新，体积小
        
        ### React
        - **包名**: `featbit-react-client-sdk`
        - **安装**: `npm install featbit-react-client-sdk`
        - **文档**: https://docs.featbit.co/sdk/client-side-sdks/react
        - **仓库**: https://github.com/featbit/featbit-react-client-sdk
        - **特点**: React Hooks 支持，Context Provider 模式
        
        ### Android
        - **包名**: `featbit-android-sdk`
        - **安装**: `implementation 'co.featbit:featbit-android-sdk:latest'`
        - **文档**: https://docs.featbit.co/sdk/client-side-sdks/android
        - **仓库**: https://github.com/featbit/featbit-android-sdk
        - **特点**: 原生 Android 支持，低内存占用
        
        ---
        
        ## 🖥️ 服务端 SDK
        
        服务端 SDK 适用于后端服务、API、微服务等场景，提供高性能和安全性。
        
        ### C# / .NET
        - **包名**: `FeatBit.ServerSdk`
        - **安装**: `dotnet add package FeatBit.ServerSdk`
        - **文档**: https://docs.featbit.co/sdk/server-side-sdks/dotnet
        - **仓库**: https://github.com/featbit/featbit-dotnet-sdk
        - **支持版本**: .NET 6.0+, .NET Framework 4.6.2+
        - **特点**: 异步支持，依赖注入友好，高性能
        
        ### Node.js
        - **包名**: `featbit-node-server-sdk`
        - **安装**: `npm install featbit-node-server-sdk`
        - **文档**: https://docs.featbit.co/sdk/server-side-sdks/node-js
        - **仓库**: https://github.com/featbit/featbit-node-server-sdk
        - **特点**: TypeScript 支持，Promise/Async-Await
        
        ### Python
        - **包名**: `featbit-python-sdk`
        - **安装**: `pip install featbit-python-sdk`
        - **文档**: https://docs.featbit.co/sdk/server-side-sdks/python
        - **仓库**: https://github.com/featbit/featbit-python-sdk
        - **支持版本**: Python 3.7+
        - **特点**: 类型提示，简洁 API
        
        ### Java
        - **包名**: `featbit-java-server-sdk`
        - **安装**: `implementation 'co.featbit:server-sdk-java:latest'`
        - **文档**: https://docs.featbit.co/sdk/server-side-sdks/java
        - **仓库**: https://github.com/featbit/featbit-java-server-sdk
        - **支持版本**: Java 8+
        - **特点**: 企业级稳定性，Spring 集成
        
        ### Go
        - **包名**: `featbit-go-server-sdk`
        - **安装**: `go get github.com/featbit/featbit-go-sdk`
        - **文档**: https://docs.featbit.co/sdk/server-side-sdks/go
        - **仓库**: https://github.com/featbit/featbit-go-sdk
        - **支持版本**: Go 1.18+
        - **特点**: 高并发，低延迟
        
        ### PHP
        - **包名**: `featbit-php-sdk`
        - **安装**: `composer require featbit/php-server-sdk`
        - **文档**: https://docs.featbit.co/sdk/server-side-sdks/php
        - **仓库**: https://github.com/featbit/featbit-php-sdk
        - **支持版本**: PHP 7.4+
        
        ---
        
        ## 🎯 如何选择合适的 SDK
        
        | 场景 | 推荐 SDK | 原因 |
        |------|---------|------|
        | 前端 Web 应用 | JavaScript/React SDK | 实时更新，用户体验好 |
        | 移动应用 | Android/iOS SDK | 原生性能，离线支持 |
        | 后端 API | 对应语言的服务端 SDK | 安全，高性能 |
        | 微服务 | 服务端 SDK | 低延迟，高可用 |
        
        ## 📚 更多资源
        
        - 官方文档: https://docs.featbit.co
        - GitHub 组织: https://github.com/featbit
        - 社区支持: https://github.com/featbit/featbit/discussions
        """;
    }

    private static string GetQuickstartGuide()
    {
        return """
        # FeatBit 快速入门指南
        
        ## 🚀 5 分钟快速开始
        
        ### 步骤 1: 创建 FeatBit 账号
        1. 访问 https://app.featbit.co
        2. 注册账号或使用现有账号登录
        3. 创建项目和环境
        
        ### 步骤 2: 创建功能开关
        1. 在控制台创建一个功能开关
        2. 设置开关的 Key（如：`new-feature`）
        3. 配置目标规则（可选）
        
        ### 步骤 3: 获取环境密钥
        1. 进入项目设置
        2. 复制环境的 Secret Key
        
        ### 步骤 4: 集成 SDK
        
        #### 以 Node.js 为例：
        
        ```bash
        npm install featbit-node-server-sdk
        ```
        
        ```javascript
        const { FbClientBuilder } = require('featbit-node-server-sdk');
        
        const client = new FbClientBuilder()
          .sdkKey('your-env-secret-key')
          .build();
        
        await client.waitForInitialization();
        
        const user = { keyId: 'user-123' };
        const isEnabled = await client.boolVariation('new-feature', user, false);
        
        console.log('功能状态:', isEnabled);
        ```
        
        ### 步骤 5: 测试和验证
        1. 运行应用
        2. 在 FeatBit 控制台切换开关状态
        3. 观察应用行为变化（实时生效）
        
        ## 🎓 下一步
        
        - 学习如何使用目标规则进行 A/B 测试
        - 了解用户分群和百分比发布
        - 探索 FeatBit 的高级特性
        """;
    }

    private static string GetBestPractices()
    {
        return """
        # FeatBit 最佳实践
        
        ## 🏗️ 架构设计
        
        ### 1. 单例模式
        - ✅ **推荐**: 在应用中使用单例 FeatBit 客户端
        - ❌ **避免**: 为每个请求创建新的客户端实例
        
        ```csharp
        // ✅ 正确：单例注册
        services.AddSingleton<IFbClient>(sp => {
            var options = new FbOptionsBuilder(envSecret).Build();
            return new FbClient(options);
        });
        
        // ❌ 错误：每次创建新实例
        var client = new FbClient(options); // 在请求处理中
        ```
        
        ### 2. 异步初始化
        - 应用启动时等待 SDK 初始化完成
        - 使用健康检查确保 SDK 就绪
        
        ### 3. 合理的默认值
        - 始终为 `variation` 方法提供合理的默认值
        - 默认值应该是安全的、不会破坏系统的选项
        
        ## 🎯 功能开关命名
        
        ### 命名规范
        - 使用 kebab-case：`new-checkout-flow`
        - 描述性强：`enable-dark-mode` 而不是 `flag1`
        - 包含环境信息（如需要）：`prod-beta-feature`
        
        ### 分类管理
        - 使用标签组织相关开关
        - 定期清理不再使用的开关
        
        ## 🔐 安全性
        
        ### 1. 密钥管理
        - ✅ 使用环境变量存储密钥
        - ✅ 客户端使用 Client-Side Key
        - ✅ 服务端使用 Server-Side Secret
        - ❌ 不要在代码中硬编码密钥
        - ❌ 不要在客户端代码中使用服务端密钥
        
        ### 2. 用户隐私
        - 避免在用户属性中存储敏感信息
        - 使用哈希或匿名 ID
        
        ## 📊 性能优化
        
        ### 1. 本地缓存
        - SDK 自动缓存功能开关数据
        - 离线模式下使用缓存值
        
        ### 2. 减少网络请求
        - 批量查询多个开关
        - 合理设置轮询间隔
        
        ### 3. 监控和告警
        - 监控 SDK 初始化状态
        - 记录异常和失败情况
        - 设置告警规则
        
        ## 🧪 测试策略
        
        ### 1. 单元测试
        ```csharp
        // 使用模拟客户端进行测试
        var mockClient = new Mock<IFbClient>();
        mockClient.Setup(c => c.BoolVariation("test-flag", It.IsAny<FbUser>(), false))
                  .Returns(true);
        ```
        
        ### 2. 集成测试
        - 使用测试环境的密钥
        - 创建专门的测试开关
        
        ### 3. 金丝雀发布
        - 先对小部分用户启用
        - 监控错误率和性能
        - 逐步扩大发布范围
        
        ## 🔄 生命周期管理
        
        ### 功能开关的生命周期
        1. **创建**: 新功能开发时创建
        2. **测试**: 在测试环境验证
        3. **发布**: 逐步向生产环境发布
        4. **稳定**: 功能稳定后保持开启
        5. **清理**: 移除代码中的开关逻辑
        6. **归档**: 在 FeatBit 中归档或删除
        
        ### 技术债务管理
        - 定期审查长期存在的开关
        - 为临时开关设置过期提醒
        - 在代码中添加 TODO 注释标记清理时间
        
        ## 📈 监控和分析
        
        ### 1. 日志记录
        ```csharp
        logger.LogInformation("功能开关 {FlagKey} 对用户 {UserId} 返回 {Value}", 
            flagKey, user.KeyId, value);
        ```
        
        ### 2. 指标收集
        - 跟踪开关使用频率
        - 分析用户分布
        - 监控性能影响
        
        ### 3. A/B 测试分析
        - 定义明确的成功指标
        - 收集足够的样本数据
        - 使用统计学方法验证结果
        
        ## 🚨 故障处理
        
        ### 降级策略
        - SDK 连接失败时使用本地缓存
        - 网络异常时返回安全的默认值
        - 实现熔断机制避免级联故障
        
        ### 应急预案
        - 准备手动开关切换流程
        - 建立快速回滚机制
        - 保持团队沟通渠道畅通
        """;
    }

    private static string GetDefaultDocumentation()
    {
        return """
        # FeatBit 文档资源
        
        欢迎使用 FeatBit MCP Server！
        
        ## 可用的文档类型
        
        使用以下 URI 访问不同的文档：
        
        - `featbit://docs/sdks` - SDK 完整列表和说明
        - `featbit://docs/quickstart` - 快速入门指南
        - `featbit://docs/best-practices` - 最佳实践
        
        ## 使用 MCP Tools
        
        您也可以使用以下工具获取信息：
        
        - `GetSDKs` - 查询可用的 SDK（可按语言筛选）
        - `GenerateIntegrationCode` - 生成集成代码示例
        
        ## 更多信息
        
        - 官方网站: https://featbit.co
        - 官方文档: https://docs.featbit.co
        - GitHub: https://github.com/featbit
        """;
    }
}
