extends SceneTree

func _init() -> void:
	var frog := ["idle", "catching", "eating", "angry"]
	var fruits := ["cherry", "strawberry", "grape", "mandarin", "apple", "pear", "peach", "pineapple", "melon", "watermelon", "trash"]
	for name in frog:
		var tex := load("res://Game/assets/frogs/" + name + ".png") as Texture2D
		if tex == null:
			push_error("MISSING frog: " + name)
		else:
			print("frog ok: ", name)
	for name in fruits:
		var tex := load("res://Game/assets/fruits/" + name + ".png") as Texture2D
		if tex == null:
			push_error("MISSING fruit: " + name)
		else:
			print("fruit ok: ", name)
	quit()