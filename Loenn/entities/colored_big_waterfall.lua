local waterfallHelper = require("helpers.waterfalls")
local utils = require("utils")

local waterfall = {}

waterfall.name = "pandorasBox/coloredBigWaterfall"
waterfall.depth = -9999
waterfall.fieldInformation = {
    color = {
        fieldType = "color",
        allowXNAColors = true,
    },
    layer = {
        options = {
            Foreground = "FG",
            Background = "BG"
        }
    }
}
waterfall.placements = {
    name = "waterfall",
    data = {
        width = 16,
        height = 64,
        layer = "FG",
        color = "LightSkyBlue"
    }
}

function waterfall.sprite(room, entity)
    local rawColor = entity.color or "LightSkyBlue"
    local color = utils.getColor(rawColor)

    local fillColor = {color[1] * 0.3, color[2] * 0.3, color[3] * 0.3, 0.3}
    local borderColor = {color[1] * 0.8, color[2] * 0.8, color[3] * 0.8, 0.8}

    return waterfallHelper.getBigWaterfallSprite(room, entity, fillColor, borderColor)
end

waterfall.rectangle = waterfallHelper.getBigWaterfallRectangle

return waterfall