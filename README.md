# QiniuBackup - 七牛备份工具

　　由于七牛官方提供的同步工具 qrsync 仅是上传同步，无法将 bucket 中的文件批量下载到本地，于是为了备份整个 bucket 所有文件，只能依据官方提供的 API 接口，写出了这个 Qiniu Backup 下载备份工具。工具原理很简单，就是读取 bucket 中的文件列表，然后一个个下载到本地。如果使用的过程中有任何问题，欢迎反馈。  
　　**使用前请先配置空间信息！**

#### 配置说明：

+ `AccessKey`：密钥组合
+ `SecretKey`：密钥组合
+ `Bucket`：空间名
+ `Domain`：使用哪个域名来访问资源
+ `Private`：是否为私有空间
+ `Prefix`：资源前缀，如有填写则只下载匹配的文件，留空则下载所有文件
+ `SaveAs`：本地保存路径
+ `OverWrite`：覆盖本地文件，如果下载中断，关闭这个选项后重新下载，可避免流量浪费

#### 运行环境：  

+ Windows x86 / x64
+ .NET Framework 2.0 +


#### 开发工具：

+ Visual Studio 2013


#### 运行说明：

+ 如果您无需重新编译代码，可直接运行 `QiniuBackupbin\Debug\QiniuBackup.exe`


#### 项目依赖：

+ 本程序依赖 `QiniuBackup.exe.config` 来记录配置信息，请确保运行目录中含有该文件；
+ 本程序依赖 `LitJson.dll` 来处理 JSON，请确保运行目录中含有该文件；
