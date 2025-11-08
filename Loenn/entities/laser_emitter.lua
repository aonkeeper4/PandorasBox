local laserEmitter = {}

local directions = {
    "Left",
    "Right",
    "Up"
}

laserEmitter.name = "pandorasBox/laserEmitter"
laserEmitter.depth = 50
laserEmitter.fieldInformation = {
    color = {
        fieldType = "color",
        allowXNAColors = true,
    },
    direction = {
        options = directions,
        editable = false
    }
}
laserEmitter.placements = {}

for _, direction in ipairs(directions) do
    table.insert(laserEmitter.placements, {
        name = string.lower(direction),
        data = {
            flag = "",
            color = "White",
            direction = direction,
            inverted = false,
            beamDuration = -1
        }
    })
end

laserEmitter.texture = "objects/pandorasBox/laser/emitter/idle0"
laserEmitter.justification = {0.5, 1.0}

return laserEmitter