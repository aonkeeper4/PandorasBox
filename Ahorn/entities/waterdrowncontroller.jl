module PandorasBoxWaterDrownController

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/waterDrowningController" WaterDrowningController(x::Integer, y::Integer, mode="Swimming", maxDuration::Number=10.0)

const placements = Ahorn.PlacementDict(
    "Water Drowning Controller (Pandora's Box)" => Ahorn.EntityPlacement(
        WaterDrowningController
    )
)

const modes = String["Swimming", "Diving"]
const iconMatrix = [
    0 0 0 0 0 1 1 0 0 0 0 0 0 0 0 0;
    0 0 0 0 1 2 0 1 0 0 0 0 0 0 0 0;
    0 0 0 0 1 0 0 1 0 0 0 0 0 0 0 0;
    0 0 0 0 0 1 1 0 0 0 0 0 0 0 0 0;
    0 0 1 1 1 0 0 0 0 1 1 0 0 0 0 0;
    0 1 0 0 0 1 0 1 1 0 0 1 1 0 0 0;
    1 0 2 2 0 0 1 0 0 2 0 0 0 1 0 0;
    1 0 2 0 0 0 1 0 2 2 2 0 0 0 1 0;
    1 0 0 0 0 0 1 2 2 2 0 0 0 0 1 0;
    0 1 0 0 0 1 0 0 2 0 0 0 0 0 0 1;
    0 0 1 1 1 0 0 0 0 0 0 0 0 0 0 1;
    0 0 0 0 0 1 0 0 0 0 0 0 0 0 1 0;
    0 0 0 0 0 1 0 0 0 0 0 0 0 0 1 0;
    0 0 0 0 0 0 1 0 0 0 0 0 0 1 0 0;
    0 0 0 0 0 0 0 1 1 0 0 1 1 0 0 0;
    0 0 0 0 0 0 0 0 0 1 1 0 0 0 0 0;
]

const iconSurface = Ahorn.matrixToSurface(
    iconMatrix,
    [
        (0 / 255, 0 / 255, 0 / 255, 0.0),
        (192 / 255, 224 / 255, 247 / 255, 1.0),
        (255 / 255, 255 / 255, 255 / 255, 1.0),
    ]
)

Ahorn.editingOptions(entity::WaterDrowningController) = Dict{String, Any}(
    "mode" => modes
)

function Ahorn.selection(entity::WaterDrowningController)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 10, y - 10, 20, 20)
end

tintColor = (137, 251, 255, 255) ./ 255

# TODO - Better render
function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::WaterDrowningController, room::Maple.Room)
    Ahorn.Cairo.save(ctx)

    Ahorn.set_antialias(ctx, 1)
    Ahorn.set_line_width(ctx, 1);

    Ahorn.drawCircle(ctx, 0, 0, 10, (1.0, 1.0, 1.0, 1.0))
    Ahorn.drawImage(ctx, iconSurface, -8, -8)

    Ahorn.Cairo.restore(ctx)
end

end