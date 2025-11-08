local xnaColors = require("consts.xna_colors")
local utils = require("utils")

local water = {}


local function getEntityColor(entity)
    local rawColor = entity.color or "LightSkyBlue"
    local color = utils.getColor(rawColor) or xnaColors.LightSkyBlue

    return color
end

water.name = "pandorasBox/coloredWater"
water.depth = 0
water.fieldOrder = {
    "x", "y", "width", "height",
    "color",
    "hasTop", "hasBottom", "hasLeft", "hasRight",
    "hasTopRays", "hasBottomRays", "hasLeftRays", "hasRightRays",
    "canJumpOnSurface"
}
water.fieldInformation = {
    color = {
        fieldType = "color",
        allowXNAColors = true,
    }
}
water.placements = {
    name = "water",
    data = {
        width = 8,
        height = 8,

        hasTop = true,
        hasBottom = false,
        hasLeft = false,
        hasRight = false,

        hasTopRays = true,
        hasBottomRays = true,
        hasLeftRays = true,
        hasRightRays = true,

        canJumpOnSurface = true,

        color = "LightSkyBlue"
    }
}

water.fillColor = function(room, entity)
    local color = getEntityColor(entity)

    return {color[1] * 0.3, color[2] * 0.3, color[3] * 0.3, 0.6}
end

water.borderColor = function(room, entity)
    local color = getEntityColor(entity)

    return {color[1] * 0.8, color[2] * 0.8, color[3] * 0.8, 0.8}
end

return water