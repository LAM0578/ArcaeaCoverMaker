# Arcaea-Cover-Maker
> 该版本的 Arcaea-Cover-Maker 是 <https://github.com/LAM0578/Arcaea-Cover-Maker> 的重写版本 (同时也进行了重新设计)

Arcaea-Cover-Maker 是一款用于制作 Arcaea 自制视频封面的软件

**更新日志**
=
>2023/3/20 (LAM)
- 上传到 github

**使用教程**  
=
*以下路径全部使用相对路径*  
*文件夹及文件会在程序启动时创建*

*Songlist.json 中需要的内容与本体 Songlist 完全一致, 可以放心复制粘贴*

---
路径说明
-
- 曲目背景放到 Background 文件夹下, 如果使用的曲目没有对应背景将使用默认背景
- 曲目放到 Songs 文件夹下, 如果找不到曲目所在的文件夹会使用默认曲绘 (文件夹的读取请看下面 Config.json 文件说明中的 read_remote_dl_with_head)
- 字体放到 Fonts 文件夹下, 在 Config.json 中填写时只需要填写文件名称即可 (如果文件夹套娃则填写相对路径)

---

Config.json 文件说明
-
~~~jsonc
{
    "index": int, // 搜索时使用的曲目索引值 (对应曲目在 songlist 中的 idx)
    "title": string, // 搜索时使用的曲目标题
    "read_remote_dl_with_head": boolean, // 读取在 songlist 中 remote_dl 为 true 的曲目时是否带 dl_ 前缀读取
    "localized": string, // 本地化标识 (与 songlist 中对应)
    "rating_class": int, // 难度标识 (暂不支持自定义颜色, 小于 0 或大于 2 会自动设置为 0 (Past))
    "top_title_ascii": string (ASCII), // 左上角标题
    "title_font_file_path": string, // 标题字体文件路径
    "artist_font_file_path": string, // 曲师字体文件路径
    "custom_difficult": string (ASCII), // 自定义难度
    "hotkey_config": { // 热键设置
        // 下面会列出可用的热键对象及默认值, 一般不需要更改
        // 如需更改请参阅以下链接:
        // Modifier: https://learn.microsoft.com/zh-cn/dotnet/api/system.windows.input.modifierkeys
        // Key: https://learn.microsoft.com/zh-cn/dotnet/api/system.windows.input.key
        "Hotkeys": {
            "Reload": { // 重新载入 (重新读取 Config.json 和 Songlist.json)
                "Modifier": 2, // ModifierKeys.Control
                "Key": 61 // Key.R
            },
            "Capture": { // 截图画面内容并保存至 Captures 文件夹下
                "Modifier": 2, // ModifierKeys.Control
                "Key": 62 // Key.S
            },
            "RatioBili": { // 使用 Bilibili 视频封面比例 (16:10)
                "Modifier": 1, // ModifierKeys.Alt
                "Key": 45 // Key.B
            },
            "RatioYtb": { // 使用 Youtube 视频封面比例 (16:9)
                "Modifier": 1, // ModifierKeys.Alt
                "Key": 68 // Key.Y
            }
        }
    }
}
~~~
---
曲目搜索说明
-

_**曲目查找结果将会使用所有曲目中匹配的最后一项, 如果两种方法都查找不到曲目不会报错**_ 

在 title 的值不为空或 null 的情况下:
- 搜索所有曲目中的 title_localized 和对应曲目中与 rating_class 对应的难度中的 title_localized 是否有和 title 值相匹配的对象, 如果搜索不到将会使用 index 进行搜索

反之:
- 使用 index 进行搜索

---
字体路径说明
-

_**字体文件名称尽量都为 ASCII 字符, 使用非 ASCII 字符可能导致锟斤拷发生**_

如果 title_font_file_path 使用的文件不存在会尝试使用 artist_font_file_path 的文件, 反过来也一样, 如都不存在会使用默认字体
> 如果出现有字显示不出来一般都是字体问题, 如果觉得这个比较烦人可以使用 [这个版本](https://github.com/LAM0578/Arcaea-Cover-Maker)

---