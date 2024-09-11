# Notes

### Handling Tag Reactions

#### Example Reaction

`lava + [meltable] -> lava + [meltable]_molten` which for this example will be `lava + iron -> lava + iron_molten`

1. Check if the `Input2` has the `meltable` tag, `iron` does contain the `meltable` tag
2. Store the name of the material with the `[meltable]` tag as `StoredMat`, which is `iron`
3. Split and store the substring of `Output2` as `StoredSub`, which is `_molten`
4. Replace the `Input1` material with `Output1`, which is is `lava`
5. Replace `Input2` with `StoredMat` + `StoredSub`, which combined becomes `iron_molten`

#### Process Order Tree (Pseudocode)

* Key
  * `P` - Pixel that is attempting reactions
  * `N` - Pixel chosen to be reacted with
  * `PM` - Material of `P`
  * `NM` - Material of `N`

```
for (R in PM.Reactions) {
    // Input is a [tag], e.g. [flammable]
    if (IsTag(R.Input1)) {

    }

    // Input is a [tag]_substr, e.g. [meltable]_molten
    else if (ContainsTag(R.Input1)) {

    }

    // Input matches the neighbor's ID
    else if (R.Input1 == N.ID) {

    }
}
```



# Ideas

### Fun Stuff

* Logo where sand falls to fill in the letters/symbols