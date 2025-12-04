# LyuEModbus

基于 NModbus 封装的 Modbus TCP 通信库，支持主站和从站模式，提供链式配置、自动重连、心跳检测、轮询等功能。

## 安装

```bash
dotnet add package LyuEModbus
```

---

## 快速开始

### 方式一：使用工厂

```csharp
// 1. 创建工厂（不要反复new，最好做成单例）
var factory = new EModbusFactory(); //重载构造里提供ILoggerFactory

// 2. 创建主站并配置
var master = factory.CreateTcpMaster("PLC1")
    .WithEndpoint("192.168.xxx.xxx", 502)  
    .WithSlaveId(1)                       // 从站 ID
    .WithTimeout(3000);                   // 超时时间（毫秒）

// 3. 连接
await master.ConnectAsync();

// 4. 读写数据后续介绍
// 5. 断开连接
master.Disconnect();

// 6. 释放资源
factory.Dispose();
```

### 方式二：使用依赖注入（推荐）

```csharp
//在此之前配置好日志服务。
services.AddZLogger();

//会自动从 DI 容器获取 `ILoggerFactory`
services.AddModbus(options =>
{
    //预加入主站
    options.AddTcpMaster("PLC1", master =>
    {
        master.IpAddress = "192.168.xxx.xxx";
        master.Port = 502;
        master.SlaveId = 1;
        master.ReadTimeout = 3000;
        master.WriteTimeout = 3000;
    });
});

// 在需要的地方注入
public class MyService
{
    private readonly IEModbusFactory _factory;
    public MyService(IEModbusFactory factory)
    {
        _factory = factory;
    }
    
    public async Task DoWork()
    {
        var master = _factory.GetMaster("PLC1");
        await master!.ConnectAsync();
        // ...
    }
}
```

---

## 主站（Master）详细用法(链式)

### 基础配置

```csharp
var master = factory.CreateTcpMaster("PLC1")
    .WithEndpoint("192.168.1.100", 502)  // 必须：IP 和端口
    .WithSlaveId(1)                       // 必须：从站 ID
    .WithTimeout(readTimeoutMs: 3000, writeTimeoutMs: 5000);
    .WithHoldingRegisterPolling(.....) // 轮询保持寄存器任务 //WithCoilPolling线圈轮询//WithPolling自定义轮询
    .WithPollerGroup(group =>//轮询组
    {
        // 每秒读取地址 0-9 的保持寄存器
        group.AddHoldingRegisters("温度数据", 0, 10, 1000, async data =>//字典
        {
            foreach (var (addr, value) in data)
                Console.WriteLine($"地址{addr} = {value}");
        });
        // 每 500ms 读取单个寄存器
        group.AddHoldingRegister("压力", 100, 500, async value =>
        {
            Console.WriteLine($"压力: {value}");
        });
        // 每 2 秒读取线圈
        group.AddCoils("开关", 0, 8, 2000, async data =>
        {
            Console.WriteLine($"开关状态: {string.Join(",", data.Values)}");
        });
        // 错误处理
        group.OnError(async (taskName, ex) =>
        {
            Console.WriteLine($"[{taskName}] 出错: {ex.Message}");
        });
        
        // 设置基础循环间隔（默认 100ms）
    	group.WithBaseInterval(50);
        
    }, autoStart: true); //自动开启
    .WithHeartbeat(300) //心跳检测（十分强烈建议配置，可以响应状态改变事件）//重载可以添加Func回调
    .OnStateChanged(state =>{})//状态改变事件，需要搭配心跳检测
    .OnReconnecting((attempt, max) =>{},3000,10)//重连事件，每3秒执行一次，最大10次
    .OnReconnectFailed(()=>{})//重连失败事件
        
    
    //轮询组任务控制
    master.PollerGroup?.Stop();       // 停止所有
    master.PollerGroup?.Start();      // 启动所有
    master.PollerGroup?.PauseAll();   // 暂停所有
    master.PollerGroup?.ResumeAll();  // 恢复所有

    master.PollerGroup?.Pause("温度数据");   // 暂停指定任务
    master.PollerGroup?.Resume("温度数据");  // 恢复指定任务
    master.PollerGroup?.Remove("温度数据");  // 移除任务
    
	//单一轮询控制器
    master.Poller?.Pause();
	//...

    // 手动停止重连
    master.StopReconnect();
        
```



### 读写操作





---

## 从站（Slave）详细用法(仅用于模拟从站，没有PLC的情况)

### 基础配置

```csharp
var slave = factory.CreateTcpSlave("Slave1")
    .WithEndpoint("0.0.0.0", 502)           // 监听地址和端口
    .WithSlaveId(1)                          // 从站 ID
    .WithDataStore(                          // 数据存储区大小
        holdingRegisterCount: 100,           // 保持寄存器数量
        coilCount: 100                       // 线圈数量
    )
    .WithChangeDetectionInterval(100);       // 数据变化检测间隔（毫秒）
```

### 事件监听

```csharp
// 运行状态变化
slave.OnRunningChanged(async isRunning =>
{
    Console.WriteLine($"从站运行状态: {(isRunning ? "运行中" : "已停止")}");
});

// 客户端连接
slave.OnClientConnected(async clientInfo =>
{
    Console.WriteLine($"客户端连接: {clientInfo}");
});

// 客户端断开
slave.OnClientDisconnected(async clientInfo =>
{
    Console.WriteLine($"客户端断开: {clientInfo}");
});

// 保持寄存器被写入（主站写入时触发）
slave.OnHoldingRegisterWritten(async (address, oldValue, newValue) =>
{
    Console.WriteLine($"寄存器 {address}: {oldValue} -> {newValue}");
});

// 线圈被写入
slave.OnCoilWritten(async (address, value) =>
{
    Console.WriteLine($"线圈 {address}: {value}");
});
```

### 启动和停止

```csharp
// 启动从站
await slave.StartAsync();

// 停止从站
slave.Stop();
```

### 读写数据

```csharp
// 设置保持寄存器值（供主站读取）
slave.SetHoldingRegister(address: 0, value: 100);

// 批量设置保持寄存器
slave.SetHoldingRegisters(startAddress: 0, values: new ushort[] { 100, 200, 300 });

// 读取保持寄存器值
ushort[]? values = slave.ReadHoldingRegisters(startAddress: 0, count: 10);

// 设置线圈值
slave.SetCoil(address: 0, value: true);
```

### 完整示例

```csharp
var factory = new EModbusFactory();

var slave = factory.CreateTcpSlave("Slave1")
    .WithEndpoint("0.0.0.0", 502)
    .WithSlaveId(1)
    .WithDataStore(100, 100)
    .OnRunningChanged(async running => Console.WriteLine($"运行: {running}"))
    .OnClientConnected(async client => Console.WriteLine($"连接: {client}"))
    .OnClientDisconnected(async client => Console.WriteLine($"断开: {client}"))
    .OnHoldingRegisterWritten(async (addr, old, @new) => 
        Console.WriteLine($"寄存器 {addr}: {old} -> {@new}"));

// 初始化数据
slave.SetHoldingRegisters(0, new ushort[] { 100, 200, 300, 400, 500 });

// 启动从站
await slave.StartAsync();

// 模拟数据更新
var timer = new Timer(_ =>
{
    var random = new Random();
    slave.SetHoldingRegister(0, (ushort)random.Next(0, 1000));
}, null, 0, 1000);

Console.ReadKey();

slave.Stop();
factory.Dispose();
```

---

## 日志配置

库支持 `Microsoft.Extensions.Logging`，可以集成任何日志框架。

### 使用工厂时配置日志

```csharp
using Microsoft.Extensions.Logging;

// 使用 LoggerFactory
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

var factory = new EModbusFactory(loggerFactory);
```

### 



---

## 工厂管理

```csharp
var factory = new EModbusFactory();

// 创建主站/从站
var master1 = factory.CreateTcpMaster("PLC1");
var master2 = factory.CreateTcpMaster("PLC2");
var slave1 = factory.CreateTcpSlave("Slave1");

// 获取已创建的实例
var master = factory.GetMaster("PLC1");
var slave = factory.GetSlave("Slave1");

// 获取或创建（如果不存在则创建）
var master3 = factory.GetOrCreateTcpMaster("PLC3");

// 获取所有实例
var allMasters = factory.GetAllMasters();
var allSlaves = factory.GetAllSlaves();

// 移除并释放
factory.RemoveMaster("PLC1");
factory.RemoveSlave("Slave1");

// 释放所有资源
factory.Dispose();
```

---

## License

MIT
