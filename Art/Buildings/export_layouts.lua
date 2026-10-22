-- 건물 Aseprite 파일(Floor·Wall·Furniture·Props 타일맵)의 배치를 칸마다 타일 이름으로 꺼낸다.
-- Unity 메뉴 Dust Scrapper > Buildings > Import Buildings가 이 JSON을 읽어 건물 프리팹을 만든다.
--
-- 실행 (프로젝트 폴더에서):
--   aseprite -b --script-param dir=Art/Buildings --script Art/Buildings/export_layouts.lua

local dir = app.params["dir"]
if not dir then error("dir 매개변수가 필요하다") end
local outDir = dir .. "/Layouts"
app.fs.makeAllDirectories(outDir)

local function quote(s) return '"' .. s .. '"' end

for _, file in ipairs(app.fs.listFiles(dir)) do
  if file:match("%.aseprite$") then
    local spr = app.open(app.fs.joinPath(dir, file))
    local grid = spr.gridBounds
    local cols, rows = spr.width // grid.width, spr.height // grid.height
    local layers = {}
    for _, layer in ipairs(spr.layers) do
      if layer.isTilemap then
        local ts = layer.tileset
        local cells = {}
        for i = 1, cols * rows do cells[i] = quote("") end
        local cel = layer:cel(1)
        if cel then
          local im = cel.image
          local ox, oy = cel.position.x // grid.width, cel.position.y // grid.height
          for y = 0, im.height - 1 do
            for x = 0, im.width - 1 do
              local idx = app.pixelColor.tileI(im:getPixel(x, y))
              local cx, cy = x + ox, y + oy
              if idx > 0 and cx >= 0 and cy >= 0 and cx < cols and cy < rows then
                cells[cy * cols + cx + 1] = quote(ts:tile(idx).data or "")
              end
            end
          end
        end
        layers[#layers + 1] = string.format('    {"name": "%s", "cells": [%s]}', layer.name, table.concat(cells, ","))
      end
    end
    local name = file:gsub("%.aseprite$", "")
    local f = io.open(outDir .. "/" .. name .. ".json", "w")
    -- cells는 위쪽 줄부터, 줄마다 왼쪽부터
    f:write(string.format('{\n  "name": "%s",\n  "cols": %d,\n  "rows": %d,\n  "layers": [\n%s\n  ]\n}\n',
      name, cols, rows, table.concat(layers, ",\n")))
    f:close()
    spr:close()
    print(name .. " " .. cols .. "x" .. rows)
  end
end
