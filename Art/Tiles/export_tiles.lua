-- 테마 타일 원본(.aseprite)의 타일을 한 장씩 PNG로 꺼내 Unity 폴더에 넣는다.
-- 타일 이름은 Aseprite 타일의 사용자 데이터(tile.data)를 쓴다. 같은 이름의 PNG를 덮어쓰므로
-- Unity 쪽 타일 에셋 연결(.meta)은 그대로 남고, 그림만 바뀐다.
--
-- 실행 (프로젝트 폴더에서):
--   aseprite -b --script-param src=Art/Tiles/Ruin.aseprite --script-param out=Assets/Art/Tiles/Ruin --script Art/Tiles/export_tiles.lua

local src = app.params["src"]
local out = app.params["out"]
if not src or not out then error("src, out 매개변수가 필요하다") end

local spr = app.open(src)
local order = {}
for _, layer in ipairs(spr.layers) do
  if layer.isTilemap then
    local ts = layer.tileset
    local dir = out .. "/Sprites/" .. layer.name
    app.fs.makeAllDirectories(dir)
    local names = {}
    for i = 1, #ts - 1 do
      local tile = ts:tile(i)
      local name = tile.data
      if name == nil or name == "" then name = string.format("%s_%03d", layer.name, i) end
      tile.image:saveAs(dir .. "/" .. name .. ".png")
      names[#names + 1] = '"' .. name .. '"'
    end
    order[#order + 1] = string.format('    {"name": "%s", "tiles": [%s]}', layer.name, table.concat(names, ","))
    print(layer.name .. ": " .. (#ts - 1))
  end
end
spr:close()

-- 타일셋 순서(팔레트에 늘어놓는 순서)
local f = io.open(out .. "/tile_order.json", "w")
f:write('{\n  "layers": [\n' .. table.concat(order, ",\n") .. "\n  ]\n}\n")
f:close()
