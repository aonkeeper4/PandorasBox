module PandorasBoxDreamDashController

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/dreamDashController" DreamDashController(x::Integer, y::Integer, allowSameDirectionDash::Bool=false, allowDreamDashRedirect::Bool=true, overrideDreamDashSpeed::Bool=false, neverSlowDown::Bool=false, useEntrySpeedAngle::Bool=false, bounceOnCollision::Bool=false, stickOnCollision::Bool=false, overrideColors::Bool=false, sameDirectionSpeedMultiplier::Number=1.0, dreamDashSpeed::Number=240.0, activeBackColor::String="Black", disabledBackColor::String="af2e2d", activeLineColor::String="White", disabledLineColor::String="6a8480", particleLayer0Colors::String="ffef11,ff00d0,08a310", particleLayer1Colors::String="5fcde4,7fb25e,e0564c", particleLayer2Colors::String="5b6ee1,CC3B3B,7daa64")

const placements = Ahorn.PlacementDict(
    "Dream Dash Controller (Pandora's Box)" => Ahorn.EntityPlacement(
        DreamDashController
    )
)

const colors = sort(collect(keys(Ahorn.XNAColors.colors)))

Ahorn.editingOptions(entity::DreamDashController) = Dict{String, Any}(
    "activeBackColor" => colors,
    "disabledBackColor" => colors,
    "activeLineColor" => colors,
    "disabledLineColor" => colors
)

const iconMatrix = [
    0 0 0 0 0 0 0 1 1 1 1 1 1 1 1 1 1 0 0 0 0 0 0 0
    0 0 0 0 0 1 1 2 2 2 5 2 2 4 4 4 2 1 1 0 0 0 0 0
    0 0 0 0 1 1 2 2 2 2 2 2 4 4 2 4 4 2 1 1 0 0 0 0
    0 0 0 1 2 2 2 7 2 6 2 2 2 4 4 4 2 2 2 2 1 0 0 0
    0 0 1 7 2 2 2 2 6 6 6 2 2 2 4 2 2 2 6 2 2 1 0 0
    0 1 1 2 2 2 2 2 2 6 2 2 2 2 2 2 2 2 2 2 2 1 1 0
    0 1 2 2 2 2 2 2 2 2 2 1 1 2 2 2 2 2 2 7 2 2 1 0
    1 2 2 2 2 4 2 2 1 1 2 1 1 2 1 1 2 2 7 7 7 2 2 1
    1 2 2 5 2 2 2 2 1 1 1 1 1 1 1 1 2 7 7 2 7 7 2 1
    1 2 5 5 5 2 2 2 3 1 1 1 1 1 1 3 2 2 7 7 7 2 2 1
    1 2 2 5 2 2 2 1 1 1 1 3 3 1 1 1 1 2 2 7 2 2 2 1
    1 2 2 2 2 2 2 1 1 1 1 2 2 1 1 1 1 2 2 2 2 2 2 1
    1 2 8 2 2 2 2 3 3 1 1 1 1 1 1 3 3 2 2 2 2 2 2 1
    1 8 8 8 2 2 2 2 1 1 1 1 1 1 1 1 2 2 2 7 2 2 2 1
    1 2 8 2 2 2 2 2 1 1 3 1 1 3 1 1 2 2 2 2 2 2 2 1
    1 2 2 2 2 2 7 2 3 3 2 1 1 2 3 3 2 2 2 8 2 2 2 1
    1 2 2 2 2 2 2 2 2 2 2 3 3 2 2 2 2 2 2 2 2 2 2 1
    0 1 5 2 2 2 2 2 2 2 2 2 2 2 2 2 2 4 2 2 2 5 1 0
    0 1 1 2 2 2 2 2 2 2 2 2 2 2 2 2 4 4 4 2 2 1 1 0
    0 0 1 2 2 7 2 2 2 2 2 2 2 2 2 2 2 4 2 2 2 1 0 0
    0 0 0 1 2 2 2 2 2 7 2 2 2 2 6 2 2 2 2 2 1 0 0 0
    0 0 0 0 1 1 2 2 7 7 7 2 2 2 2 2 2 2 1 1 0 0 0 0
    0 0 0 0 0 1 1 2 2 7 2 2 2 2 2 2 2 1 1 0 0 0 0 0
    0 0 0 0 0 0 0 1 1 1 1 1 1 1 1 1 1 0 0 0 0 0 0 0
]

const iconSurface = Ahorn.matrixToSurface(
    iconMatrix,
    [
        (0 / 255, 0 / 255, 0 / 255, 0.0),
        (255 / 255, 255 / 255, 255 / 255, 1.0), # White
        (0 / 255, 0 / 255, 0 / 255, 1.0), # Black
        (148 / 255, 165 / 255, 165 / 255, 1.0), # Gray
        (0 / 255, 161 / 255, 4 / 255, 1.0), # Green
        (243 / 255, 242 / 255, 5 / 255, 1.0), # Yellow
        (247 / 255, 0 / 255, 213 / 255, 1.0), # Magenta
        (84 / 255, 207 / 255, 231 / 255, 1.0), # Cyan
        (212 / 255, 79 / 255, 68 / 255, 1.0), # Red


    ]
)

sprite = "objects/pandorasBox/DreamDashController/DreamDashController0"

function Ahorn.selection(entity::DreamDashController)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 12, y - 12, 24, 24)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DreamDashController, room::Maple.Room) = Ahorn.drawImage(ctx, iconSurface, -12, -12)

end