local coloredDustSprite = {}

coloredDustSprite.name = "pandorasBox/dustSpriteColorController"
coloredDustSprite.depth = 0
coloredDustSprite.fieldInformation = {
    eyeColor = {
        fieldType = "color",
        allowXNAColors = true,
    }
}
coloredDustSprite.placements = {
    name = "controller",
    data = {
        eyeTexture = "danger/dustcreature/eyes",
        eyeColor = "Red",
        borderColor = "Red,Green,Blue"
    }
}

coloredDustSprite.texture = "objects/pandorasBox/controllerIcons/dustSpriteColorController"

return coloredDustSprite