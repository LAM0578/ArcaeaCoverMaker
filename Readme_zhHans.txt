Arcaea Cover Maker 使用教程
>>>>>>>>>>>>>>>>>>>>>>>>>>
// 以下路径全部使用相对路径 //
// 文件夹及文件会在程序启动时创建 //
曲目文件放到 Songs 文件夹中 (和本体一样) 并编辑 Songlist.json (和本体一样)
背景文件放到 Backgrounds 文件夹中
>>>>>>>>>>>>>>>>>>>>>>>>>>
Config:
	[index] [Number(Int)]: 在 songlist 中的索引 (也就是曲目的 idx 值)
	[title] [String]: 曲目标题
	[read_remote_dl_with_head] [Boolean]: 读取需要下载的曲目时读取文件夹名称带 dl_ 前缀
	[localized] [String]: 本地化 与 songlist 中对应
	[rating_class] [Number(Int)]: 难度标识 (暂不支持自定义颜色 小于0或大于2会自动设置为0 (Past))
	[top_title_ascii] [String]: 左上角的标题 (字体不支持中文字符 使用中文字符导致的问题本人不承担)
	[title_font_file_path] [String]: 曲目标题 字体文件路径 (请将字体放在 Fonts 文件夹下) 如果找不到会使用默认字体
	[artist_font_file_path] [String]: 曲目作者 字体文件路径 (请将字体放在 Fonts 文件夹下) 如果找不到会使用默认字体
	[custom_difficult] [String]: 自定义难度等级 (字体不支持中文字符 使用中文字符导致的问题本人不承担)
	[hotkey_config]: 热键 一般不需要更改 如需更改请查看以下链接 并使用对应值 (Int):
		[Modifier] https://learn.microsoft.com/zh-cn/dotnet/api/system.windows.input.modifierkeys
		[Key] https://learn.microsoft.com/zh-cn/dotnet/api/system.windows.input.key
	HotkeyConfig:
		// 支持的热键操作标识及默认热键 //
		[Reload]: 重载
			[Modifier] [Number(Int)]: 2 (ModifierKeys.Control)
			[Key] [Number(Int)]: 61 (Key.R)
		[Capture]: 截图 (图片会保存到相对路径下的 Captures 文件夹中)
			[Modifier] [Number(Int)]: 2 (ModifierKeys.Control)
			[Key] [Number(Int)]: 62 (Key.S)
		[RatioBili]: Bilibili 视频封面比例 (16:10)
			[Modifier] [Number(Int)]: 1 (ModifierKeys.Alt)
			[Key] [Number(Int)]: 45 (Key.B)
		[RatioYtb]: Youtube 视频封面比例 (16:9)
			[Modifier] [Number(Int)]: 1 (ModifierKeys.Alt)
			[Key] [Number(Int)]: 68 (Key.Y)
>>>>>>>>>>>>>>>>>>>>>>>>>>