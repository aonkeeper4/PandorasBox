local laserNoteBlock = {}

local directions = {
    "Horizontal",
    "Vertical"
}

laserNoteBlock.name = "pandorasBox/laserNoteBlock"
laserNoteBlock.depth = 50
laserNoteBlock.fieldInformation = {
    direction = {
        options = directions,
        editable = false
    }
}
laserNoteBlock.placements = {}

for _, direction in ipairs(directions) do
    table.insert(laserNoteBlock.placements, {
        name = string.lower(direction),
        data = {
            direction = direction,
            pitch = 69,
            volume = 1.0,
            sound = "game_03_deskbell_again",
            atPlayer = false
        }
    })
end

local horizontalTexture = "objects/pandorasBox/laser/noteblock/noteblock_horizontal"
local verticalTexture = "objects/pandorasBox/laser/noteblock/noteblock_vertical"

function laserNoteBlock.texture(room, entity)
    local horizontal = string.lower(entity.direction or "Horizontal") == "horizontal"

    return horizontal and horizontalTexture or verticalTexture
end

return laserNoteBlock