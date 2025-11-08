local lever = {}

lever.name = "pandorasBox/lever"
lever.depth = -100
lever.placements = {
    name = "lever",
    data = {
        active = false,
        flag = ""
    }
}

lever.justification = {0.5, 1.0}

function lever.texture(room, entity)
    local active = entity.active

    return active and "objects/pandorasBox/lever/lever4" or "objects/pandorasBox/lever/lever0"
end

return lever