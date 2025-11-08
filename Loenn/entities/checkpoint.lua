local checkpoint = {}
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local drawing = require("utils.drawing")

local themes = {"flag"}

checkpoint.name = "pandorasBox/checkpoint"
checkpoint.depth = -100
checkpoint.fieldOrder = {"x", "y", "activationWidth", "activationHeight", "activationSound", "theme", "movable", "spawnConfetti"}
checkpoint.placements = {
    name = "checkpoint",
    data = {
        spawnConfetti = true,
        theme = "flag",
        activationWidth = 16,
        activationHeight = 16,
        activationSound = "event:/game/07_summit/checkpoint_confetti",
        moveable = true,
    }
}
checkpoint.fieldInformation = {
    theme = {
        options = themes,
        editable = true
    },
    activationWidth = {
        fieldType = "integer"
    },
     activationHeight = {
        fieldType = "integer"
    },
}

checkpoint.justification = {0.5, 1.0}

function checkpoint.texture(room, entity)
    local theme = entity.theme or "flag"

    return "objects/pandorasBox/checkpoint/" .. theme .. "/active_idle00"
end

function checkpoint.drawSelected(room, layer, entity, color)
    local x = entity.x
    local y = entity.y
    local activationWidth = entity.activationWidth or 16
    local activationHeight = entity.activationHeight or 16

    drawing.callKeepOriginalColor(function()
        love.graphics.setColor(0.3, 0.3, 0.3, 0.6)

        love.graphics.rectangle("fill", x - activationWidth / 2, y - activationHeight, activationWidth, activationHeight)
    end)
end

return checkpoint