local laserValve = {}

local directionRotations = {
    Right = 0,
    Down = math.pi / 2,
    Left = math.pi,
    Up = math.pi / 2 * 3
}

laserValve.name = "pandorasBox/laserValve"
laserValve.depth = 50
laserValve.fieldInformation = {
    direction = {
        options = table.keys(directionRotations),
        editable = false
    }
}
laserValve.placements = {}

for direction, _ in pairs(directionRotations) do
    table.insert(laserValve.placements, {
        name = string.lower(direction),
        data = {
            direction = direction,
            delay = 4
        }
    })
end

laserValve.texture = "objects/pandorasBox/laser/valve/valve2"

function laserValve.rotation(room, entity)
    local direction = entity.direction or "Right"
    local rotation = directionRotations[direction] or 0

    return rotation
end

return laserValve