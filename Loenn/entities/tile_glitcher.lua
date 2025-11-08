local depths = require("consts.object_depths")

local tileGlitcher = {}

local targets = {
    "None",
    "FG",
    "BG",
    "Both"
}

tileGlitcher.name = "pandorasBox/tileGlitcher"
tileGlitcher.fillColor = {0.5, 0.3, 1.0, 0.4}
tileGlitcher.borderColor = {0.5, 0.3, 1.0, 1.0}
tileGlitcher.depth = depths.fgTerrain - 1
tileGlitcher.fieldInformation = {
    target = {
        options = targets,
        editable = false
    }
}
tileGlitcher.placements = {
    name = "tile_glitcher",
    data = {
        width = 8,
        height = 8,
        allowAir = false,
        transformAir = false,
        target = "Both",
        threshold = 0.1,
        rate = 0.05,
        flag = "",
        customFgTiles = "",
        customBgTiles = ""
    }
}

return tileGlitcher