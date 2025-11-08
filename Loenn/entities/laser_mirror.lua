local laserMirror = {}

local openingScales = {
    LeftUp = {1, 1},
    UpRight = {-1, 1},
    RightDown = {-1, -1},
    DownLeft = {1, -1}
}

laserMirror.name = "pandorasBox/laserMirror"
laserMirror.depth = 50
laserMirror.fieldInformation = {
    opening = {
        options = table.keys(openingScales),
        editable = false
    }
}
laserMirror.placements = {}

for opening, _ in pairs(openingScales) do
    table.insert(laserMirror.placements, {
        name = string.lower(opening),
        data = {
            opening = opening
        }
    })
end

laserMirror.texture = "objects/pandorasBox/laser/mirror/mirror_static"

function laserMirror.scale(room, entity)
    local opening = entity.opening or "LeftUp"
    local scales = openingScales[opening] or openingScales.LeftUp

    return unpack(scales)
end

return laserMirror