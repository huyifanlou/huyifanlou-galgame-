"""Create bundled static instances from the upstream OFL variable sources (fontTools required)."""
from pathlib import Path
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

root = Path(__file__).resolve().parents[2]
sources = root / "tools/fonts/sources"
destination = root / "Game/Assets/Resources/Fonts"
for source, weight, output in [
    ("NotoSansSC-variable.ttf", 400, "MapSans.ttf"),
    ("NotoSansSC-variable.ttf", 600, "MapHeading.ttf"),
    ("NotoSerifSC-variable.ttf", 400, "StorySerif.ttf"),
]:
    font = TTFont(sources / source)
    instantiateVariableFont(font, {"wght": weight}, inplace=True, updateFontNames=True)
    font.save(destination / output)
    print(output)
