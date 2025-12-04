# LyuEModbus

基于 NModbus 封装的 Modbus TCP 通信库，支持主站和从站模式，提供链式配置、自动重连、心跳检测、轮询等功能。

项目包含Avalonia演示项目，实现部分演示如：轮询，日志，读写，批量读写等等

## 安装

```bash
dotnet add package LyuEModbus
```

---

## 快速开始

### 方式一：使用工厂

```csharp
// 1. 创建工厂（不要反复new，最好做成单例）
var factory = new EModbusFactory() //重载构造里提供ILoggerFactory
    .ConfigureDefaultMaster(opt =>
    {
        opt.IpAddress = "xxx.xxx.xxx.xxx";
        opt.Port = 502;
        opt.SlaveId = 1;
        opt.ByteOrder = ByteOrder.ABCD;
        opt.ReadTimeout = 3000;
        opt.WriteTimeout = 3000;
        //...
    });//后续创建的客户端都会用此配置

// 2. 创建主站并配置
var master = factory.CreateTcpMaster("PLC1")
    .WithEndpoint("192.168.xxx.xxx", 502)  //以下可手动替换工厂提供的默认配置
    ...//替换其他

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
    //以下2选1
    //为以后每个主站加默认配置
    options.ConfigureDefaultMaster(master =>
    {
        master.ByteOrder = ByteOrder.ABCD;
        master.ReadTimeout = 3000;
        master.WriteTimeout = 3000;
        master.AutoReconnect = true;
        master.ReconnectInterval = 3000;
        master.MaxReconnectAttempts = 5;
        //...
    });
    
    //或预加入主站
    options.AddTcpMaster("PLC1", master =>
    {
        master.IpAddress = "192.168.xxx.xxx";
        master.Port = 502;
        master.SlaveId = 1;
        master.ReadTimeout = 3000;
        master.WriteTimeout = 3000;
        //...
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
    .WithByteOrder(ByteOrder.ABCD) //字节序
    .WithTimeout(readTimeoutMs: 3000, writeTimeoutMs: 5000);
    .WithHoldingRegisterPolling(.....) // 轮询保持寄存器任务 //WithCoilPolling线圈轮询//WithPolling自定义轮询
    .WithPollerGroup(group =>//轮询组（串行执行）
    {
        // 每秒读取地址 0-9 的保持寄存器
        group.AddHoldingRegisters("温度数据", 0, 10, 1000, async data =>//字典
        {
            foreach (var (addr, value) in data)
                Console.WriteLine($"地址{addr} = {value}");
        });
            
        //浮点数轮询
		group.AddFloat("温度", 0, 500, async value => 
        {
            Console.WriteLine($"温度: {value}");
        });
        // 错误处理
        group.OnError(async (taskName, ex) =>
        {
            Console.WriteLine($"[{taskName}] 出错: {ex.Message}");
        });
        
        //。。。
        
        // 自定义轮询（任意逻辑）
        group.Add("自定义任务", 1000, async master =>
        {
            // 你可以在这里执行任何自定义操作
            var floatVal = await master.ReadFloatAsync(100, ByteOrder.ABCD);
            var intVal = await master.ReadInt32Async(102);
            // 组合处理...
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



//可从实例获得的属性
Console.WriteLine($"名称: {master.Name}");
Console.WriteLine($"地址: {master.Address}");
Console.WriteLine($"从站ID: {master.SlaveId}");
Console.WriteLine($"字节序: {master.ByteOrder}");
Console.WriteLine($"状态: {master.State}");
Console.WriteLine($"已连接: {master.IsConnected}");
        
```



### 读写操作

以下所有类型的读写方法都支持如下（默认为空）的 **错误回调** 与 **重试策略** 参数。

以读取Float数组为例：

```C#
 float[]? data = await master.ReadFloatsAsync(
     ReadAddress,//起始地址
     ReadCount,//数目
     ex =>
     {
         //todo//重试次数消耗完触发
         return Task.CompletedTask;
     }, retryCount: 3
 );
```



以下的介绍均无重试与错误回调参数，可自行添加，不再阐述

### 保持寄存器

#### 基础类型（ushort） 

```csharp
// 读取单个保持寄存器
ushort? value = await master.ReadHoldingRegisterAsync(address: 0);

// 批量读取保持寄存器（返回数组）
ushort[]? values = await master.ReadHoldingRegistersAsync(startAddress: 0, count: 10);

// 批量读取保持寄存器（返回字典，key=地址）
Dictionary<ushort, ushort>? dict = await master.ReadHoldingRegistersToDictAsync(startAddress: 0, count: 10);

// 写入单个保持寄存器
bool success = await master.WriteRegisterAsync(address: 0, value: 100);

// 批量写入保持寄存器
bool success = await master.WriteRegistersAsync(startAddress: 0, values: new ushort[] { 100, 200, 300 });

// 递增寄存器值（读取-修改-写入）
ushort? newValue = await master.IncrementRegisterAsync(address: 0, increment: 1);
```

#### Float（32位浮点数，占用2个寄存器）

```csharp
// 读取单个 Float（使用主站默认字节序）
float? value = await master.ReadFloatAsync(address: 0);

// 读取单个 Float（指定字节序）
float? value = await master.ReadFloatAsync(address: 0, byteOrder: ByteOrder.ABCD);

// 批量读取 Float
float[]? values = await master.ReadFloatsAsync(address: 0, count: 5);

// 写入单个 Float
bool success = await master.WriteFloatAsync(address: 0, value: 3.14f);

// 批量写入 Float
bool success = await master.WriteFloatsAsync(address: 0, values: new float[] { 1.1f, 2.2f, 3.3f });
```

#### Int32（32位有符号整数，占用2个寄存器）

```csharp
// 读取单个 Int32
int? value = await master.ReadInt32Async(address: 0);

// 批量读取 Int32
int[]? values = await master.ReadInt32sAsync(address: 0, count: 5);

// 写入单个 Int32
bool success = await master.WriteInt32Async(address: 0, value: 12345);

// 批量写入 Int32
bool success = await master.WriteInt32sAsync(address: 0, values: new int[] { 100, 200, 300 });
```

#### UInt32（32位无符号整数，占用2个寄存器）

```csharp
uint? value = await master.ReadUInt32Async(address: 0);
uint[]? values = await master.ReadUInt32sAsync(address: 0, count: 5);
bool success = await master.WriteUInt32Async(address: 0, value: 12345u);
bool success = await master.WriteUInt32sAsync(address: 0, values: new uint[] { 100, 200, 300 });
```

#### Double（64位浮点数，占用4个寄存器）

```csharp
double? value = await master.ReadDoubleAsync(address: 0);
double[]? values = await master.ReadDoublesAsync(address: 0, count: 5);
bool success = await master.WriteDoubleAsync(address: 0, value: 3.14159265);
bool success = await master.WriteDoublesAsync(address: 0, values: new double[] { 1.1, 2.2, 3.3 });
```

#### Int64 / UInt64（64位整数，占用4个寄存器）

```csharp
long? value = await master.ReadInt64Async(address: 0);
ulong? value = await master.ReadUInt64Async(address: 0);
bool success = await master.WriteInt64Async(address: 0, value: 123456789L);
bool success = await master.WriteUInt64Async(address: 0, value: 123456789UL);
```

#### Boolean（保持寄存器方式，1个寄存器=1个布尔值）

```csharp
// 读取 Boolean（0=false, 非0=true）
bool? value = await master.ReadBooleanAsync(address: 0);

// 批量读取 Boolean
bool[]? values = await master.ReadBooleansAsync(address: 0, count: 5);

// 写入 Boolean（true=1, false=0）
bool success = await master.WriteBooleanAsync(address: 0, value: true);

// 批量写入 Boolean
bool success = await master.WriteBooleansAsync(address: 0, values: new bool[] { true, false, true });
```



---

## 从站（Slave）详细用法(仅用于模拟从站，没有PLC的情况)

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

### 配置示例

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
        Console.WriteLine($"寄存器 {addr}: {old} -> {@new}"));//值被主站修改

//以上同主站一致可以在工厂设置默认值，不在阐述

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
