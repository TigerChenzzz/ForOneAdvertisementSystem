# ForOneAdvertisementSystem
 
## 介绍
 这是 For One 制作组的推广组件, 提供给泰拉瑞亚模组制作者使用.

## 如何使用

1. 将 ForOneAdvertisementSystem.dll 和 ForOneAdvertisementSystem.pdb (.pdb可以不用) 放入你的Mod中的 lib 文件夹下 (如果没有就创建一个)
2. 在 build.txt 中添加一行: dllReferences = ForOneAdvertisementSystem  (当你的 mod 有多个 dllReferences 时可以用 ',' 隔开)
3. 在你的项目中添加对此 dll 的引用<br/>
	对于 VS, 在资源管理器中右键依赖项 -> 添加项目引用 -> 右下角浏览 -> 找到 lib 文件夹下的 dll 文件 -> 添加<br/>
	当然还有另一种方法, 直接在 .csproj 文件中添加:
	```HTML
	<ItemGroup>
		<Reference Include="lib\*.dll" />
	</ItemGroup>
	```
4. 在你的 Mod 的 Load() 中添加一句:<br />
	```C#
	ForOneAdvertisementSystem.ForOneAdvertisementSystem.Load(this);
	```
5. 完成!

 当然, ForOneAdvertisementSystem.ForOneAdvertisementSystem.Load 还有其它重载可以允许你自定义在推广内容中你的模组的名字等, 请自行探索.
