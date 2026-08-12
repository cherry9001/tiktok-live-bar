using UnityEngine;

namespace TikTokLiveGame
{
    public sealed class PlayerActor : MonoBehaviour
    {
        private Transform body;
        private SpriteRenderer characterRenderer;
        private SpriteFlipbook flipbook;
        private SpriteRenderer avatarRenderer;
        private SpriteRenderer wingsRenderer;
        private SpriteRenderer bannerRenderer;
        private Transform wingAnchor;
        private TextMesh nameLabel;
        private TextMesh rankLabel;
        private TextMesh titleLabel;
        private TextMesh titleShadowLabel;
        private Vector3 home;
        private Vector3 normalHome;
        private float phase;
        private float actionUntil;
        private float walkStartedAt;
        private float walkDuration;
        private Vector3 walkOrigin;
        private float jumpStartedAt;
        private float jumpDuration;
        private ActionState action;
        private string characterName;
        private Vector3 characterBaseScale = Vector3.one;
        private float lineupScale = 1f;
        private float crowdScale = 1f;
        private int topRank;
        private float effectScale = 1f;
        private float growUntil;
        private float visualAlpha = 1f;
        private float targetAlpha = 1f;
        private bool giftFocused;
        private Vector3 avatarBaseScale = Vector3.one;
        private float giftFocusScale = 1f;
        private float decorationScale = 1f;
        private float decorationBadgeY = 2.3f;
        private float bannerTextOffsetY;
        private string avatarUrl;
        private bool spawnDropPending;
        private bool spawnDropping;
        private float spawnDropStartedAt;

        private const float SpawnDropHeight = 6f;
        private const float SpawnDropDuration = 1.8f;
        private const float JumpHeight = 2f;

        public string UserId { get; private set; }
        public string Nickname { get; private set; }
        public int GiftPower { get; private set; }
        public float LastActiveTime { get; private set; }
        public bool IsTopRanked => topRank > 0;
        public bool IsNpc => !string.IsNullOrEmpty(UserId) && UserId.StartsWith("npc-");
        private bool UsesSyntheticAvatar => !string.IsNullOrEmpty(UserId) &&
            (UserId.StartsWith("npc-") || UserId.StartsWith("demo-") || UserId == "master-test");

        public void Initialize(TikTokPlayerData data, Vector3 position, Color color)
        {
            UserId = data.userId;
            Nickname = string.IsNullOrWhiteSpace(data.nickname) ? data.uniqueId : data.nickname;
            GiftPower = Mathf.Max(0, data.giftPower);
            home = normalHome = position;
            transform.position = position;
            spawnDropPending = true;
            phase = Random.value * Mathf.PI * 2f;

            GameObject character = new("Dancer");
            character.transform.SetParent(transform, false);
            body = character.transform;
            characterRenderer = character.AddComponent<SpriteRenderer>();
            characterRenderer.sharedMaterial = GameMaterials.Sprite();
            characterRenderer.sortingOrder = Mathf.RoundToInt((12f - position.z) * 100f);
            flipbook = character.AddComponent<SpriteFlipbook>();
            ChangeCharacter();

            GameObject wingAnchorObject = new("Wing Anchor");
            wingAnchorObject.transform.SetParent(transform, false);
            wingAnchor = wingAnchorObject.transform;
            GameObject wingsObject = new("Wings");
            wingsObject.transform.SetParent(wingAnchor, false);
            wingsRenderer = wingsObject.AddComponent<SpriteRenderer>();
            wingsRenderer.sharedMaterial = GameMaterials.Sprite();
            wingsRenderer.sortingOrder = characterRenderer.sortingOrder - 2;
            wingsRenderer.enabled = false;

            GameObject bannerObject = new("Status Banner");
            bannerObject.transform.SetParent(transform, false);
            bannerRenderer = bannerObject.AddComponent<SpriteRenderer>();
            bannerRenderer.sharedMaterial = GameMaterials.Sprite();
            bannerRenderer.sortingOrder = characterRenderer.sortingOrder + 8;
            bannerRenderer.enabled = false;

            GameObject avatarObject = new("Avatar");
            avatarObject.transform.SetParent(transform, false);
            avatarObject.transform.localPosition = new Vector3(-0.75f, 2.1f, 0f);
            avatarRenderer = avatarObject.AddComponent<SpriteRenderer>();
            avatarRenderer.sharedMaterial = GameMaterials.CircularAvatar();
            avatarRenderer.sortingOrder = characterRenderer.sortingOrder + 12;
            avatarRenderer.enabled = false;

            GameObject labelObject = new("Name");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0.18f, 2.1f, 0f);
            nameLabel = labelObject.AddComponent<TextMesh>();
            nameLabel.text = FormatDisplayName(Nickname);
            nameLabel.anchor = TextAnchor.MiddleLeft;
            nameLabel.alignment = TextAlignment.Left;
            nameLabel.fontSize = 40;
            nameLabel.characterSize = 0.032f;
            nameLabel.fontStyle = FontStyle.Bold;
            nameLabel.color = Color.white;
            labelObject.GetComponent<MeshRenderer>().sortingOrder = characterRenderer.sortingOrder + 10;

            rankLabel = CreateLabel("Rank", new Vector3(0f, 3.05f, 0f), 48, new Color(1f, 0.82f, 0.2f), characterRenderer.sortingOrder + 15);
            rankLabel.gameObject.SetActive(false);
            titleLabel = CreateLabel("Title", new Vector3(0f, -0.25f, 0f), 36, new Color(0.3f, 0.95f, 1f), characterRenderer.sortingOrder + 14);
            titleLabel.characterSize = 0.04f;
            titleLabel.fontStyle = FontStyle.Bold;
            titleLabel.color = Color.white;
            titleLabel.gameObject.SetActive(false);
            titleShadowLabel = CreateLabel("Title Shadow", new Vector3(0f, -0.25f, 0f), 36, new Color(0f, 0f, 0f, 0.92f), characterRenderer.sortingOrder + 13);
            titleShadowLabel.characterSize = 0.04f;
            titleShadowLabel.fontStyle = FontStyle.Bold;
            titleShadowLabel.gameObject.SetActive(false);
            ApplyFallbackAvatar();
            if (!UsesSyntheticAvatar) LoadAvatar(data.avatar);
            ApplyEvolution();
            Touch();
        }

        private void ApplyFallbackAvatar()
        {
            const int avatarCount = 16;
            int avatarIndex = StableAvatarIndex(UserId, avatarCount);
            ApplyAvatar(Resources.Load<Sprite>($"NpcAvatars/npc-avatar-{avatarIndex:00}"));
        }

        private void LoadAvatar(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || url == avatarUrl) return;
            avatarUrl = url;
            string requestedUrl = url;
            AvatarService.Instance?.Load(url, sprite =>
            {
                if (this != null && avatarUrl == requestedUrl && sprite != null) ApplyAvatar(sprite);
            });
        }

        public void UpdateIdentity(TikTokEvent data)
        {
            if (data == null) return;
            string nextNickname = !string.IsNullOrWhiteSpace(data.nickname)
                ? data.nickname
                : !string.IsNullOrWhiteSpace(data.uniqueId) ? data.uniqueId : Nickname;
            if (!string.IsNullOrWhiteSpace(nextNickname) && nextNickname != Nickname)
            {
                Nickname = nextNickname;
                if (nameLabel != null) nameLabel.text = FormatDisplayName(Nickname);
            }
            if (!UsesSyntheticAvatar) LoadAvatar(data.avatar);
        }

        private static int StableAvatarIndex(string value, int count)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 16777619;
                }
                return (int)(hash % (uint)Mathf.Max(1, count));
            }
        }

        private static string FormatDisplayName(string value)
        {
            string displayName = string.IsNullOrWhiteSpace(value) ? "Dancer" : value.Trim();
            return displayName.Length <= 18 ? displayName : displayName[..17] + "…";
        }

        private TextMesh CreateLabel(string objectName, Vector3 position, int fontSize, Color color, int order)
        {
            GameObject labelObject = new(objectName);
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = position;
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = fontSize;
            label.characterSize = 0.032f;
            label.color = color;
            labelObject.GetComponent<MeshRenderer>().sortingOrder = order;
            return label;
        }

        private void ApplyAvatar(Sprite sprite)
        {
            if (this == null || avatarRenderer == null || sprite == null) return;
            avatarRenderer.sprite = sprite;
            float size = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            avatarBaseScale = Vector3.one * (size > 0f ? 0.62f / size : 1f);
            avatarRenderer.transform.localScale = avatarBaseScale;
            avatarRenderer.enabled = true;
            RefreshBadgeVisibility();
            ApplyAlpha();
        }

        public void Touch() => LastActiveTime = Time.unscaledTime;

        public void AddGiftPower(int diamonds)
        {
            GiftPower += Mathf.Max(0, diamonds);
            characterBaseScale = Vector3.one * CharacterScale() * GiftPowerScale() * lineupScale;
            ApplyEvolution();
            Touch();
        }

        private float GiftPowerScale()
        {
            // Ranking controls the size of Top 1-3. Gift totals may only make a
            // regular dancer slightly larger so nobody suddenly fills the frame.
            return topRank > 0 ? 1f : 1f + Mathf.Min(0.18f, GiftPower / 3500f);
        }

        private void ApplyEvolution()
        {
            if (titleLabel == null) return;
            string title = GiftPower >= 1000 ? "CHỦ TỊCH"
                : GiftPower >= 200 ? "VUA SÀN NHẢY"
                : GiftPower >= 50 ? "DÂN CHƠI"
                : GiftPower >= 10 ? "TÂN BINH"
                : string.Empty;
            SetTitleText(title);
            SetTitleVisible(!string.IsNullOrEmpty(title) && (giftFocused || topRank > 0 || GiftPower >= 10 || crowdScale >= 0.56f));
            RefreshDecorations();
        }

        public void ShowTitle(string title, float seconds)
        {
            if (string.IsNullOrWhiteSpace(title) || titleLabel == null) return;
            SetTitleText(FormatBannerTitle(title));
            SetTitleVisible(true);
            CancelInvoke(nameof(ApplyEvolution));
            Invoke(nameof(ApplyEvolution), Mathf.Max(2f, seconds));
        }

        private static string FormatBannerTitle(string value)
        {
            string title = value?.Trim() ?? string.Empty;
            return title.Length <= 16 ? title : title[..15] + "…";
        }

        private void SetTitleText(string value)
        {
            titleLabel.text = value;
            if (titleShadowLabel != null) titleShadowLabel.text = value;
        }

        private void SetTitleVisible(bool visible)
        {
            titleLabel.gameObject.SetActive(visible);
            if (titleShadowLabel != null) titleShadowLabel.gameObject.SetActive(visible);
        }

        public void SetTopRank(int rank)
        {
            int previousRank = topRank;
            topRank = Mathf.Clamp(rank, 0, 3);
            lineupScale = LineupScaleForRank();
            home = topRank > 0 ? RankPodiumPosition(topRank) : normalHome;
            rankLabel.text = topRank > 0 ? $"TOP {topRank}" : string.Empty;
            rankLabel.gameObject.SetActive(topRank > 0);
            characterBaseScale = Vector3.one * CharacterScale() * GiftPowerScale() * lineupScale;
            RefreshBadgeVisibility();
            if (topRank > 0 && topRank != previousRank && action != ActionState.Walk) Celebrate(3f);
        }

        private static Vector3 RankPodiumPosition(int rank)
        {
            return rank switch
            {
                1 => new Vector3(0f, 0.76f, -6.15f),
                2 => new Vector3(-2.05f, 0.76f, -6.05f),
                3 => new Vector3(2.05f, 0.76f, -6.05f),
                _ => Vector3.zero
            };
        }

        private float LineupScaleForRank()
        {
            return topRank switch
            {
                1 => Mathf.Clamp(crowdScale * 1.28f, 0.68f, 0.78f),
                2 => Mathf.Clamp(crowdScale * 1.18f, 0.64f, 0.72f),
                3 => Mathf.Clamp(crowdScale * 1.1f, 0.6f, 0.68f),
                _ => crowdScale
            };
        }

        public void SetCrowdSlot(Vector3 position, float scale)
        {
            normalHome = position;
            crowdScale = Mathf.Clamp(scale, 0.26f, 1f);
            if (topRank == 0)
            {
                home = normalHome;
            }
            if (spawnDropPending)
            {
                spawnDropPending = false;
                spawnDropping = true;
                spawnDropStartedAt = Time.time;
                transform.position = home + Vector3.up * SpawnDropHeight;
            }
            lineupScale = LineupScaleForRank();
            characterBaseScale = Vector3.one * CharacterScale() * GiftPowerScale() * lineupScale;
            RefreshBadgeVisibility();
        }

        private void RefreshBadgeVisibility()
        {
            // Keep every dancer identifiable in every row. Perspective scaling
            // already makes badges smaller toward the back, so hiding them based
            // on crowdScale made rear-row players look anonymous.
            nameLabel.gameObject.SetActive(true);
            if (avatarRenderer.sprite != null) avatarRenderer.enabled = true;
            if (titleLabel != null && !giftFocused && topRank == 0 && GiftPower < 10 && crowdScale < 0.56f) SetTitleVisible(false);
            else ApplyEvolution();
            RefreshDecorations();
        }

        private void RefreshDecorations()
        {
            if (wingsRenderer == null || bannerRenderer == null) return;
            bool decorated = giftFocused || topRank > 0 || GiftPower >= 10;
            string variant = topRank == 1 || GiftPower >= 1000 ? "royal"
                : topRank == 2 || GiftPower >= 200 ? "fire"
                : topRank == 3 || GiftPower >= 50 ? "neon"
                : "ice";
            bannerTextOffsetY = variant switch
            {
                "royal" => -0.075f,
                "fire" => -0.025f,
                _ => -0.01f
            };
            string wingsAsset = giftFocused ? "Banners/energy-wings-v2" : $"Banners/wings-{variant}";
            wingsRenderer.sprite = Resources.Load<Sprite>(wingsAsset) ?? Resources.Load<Sprite>("Banners/energy-wings-v2");
            bannerRenderer.sprite = Resources.Load<Sprite>($"Banners/title-{variant}");
            if (wingsRenderer.sprite != null)
            {
                float width = wingsRenderer.sprite.bounds.size.x;
                decorationScale = giftFocused ? Mathf.Max(0.55f, lineupScale) : lineupScale;
                float characterHeight = Mathf.Max(0.1f, VisibleCharacterHeight(characterBaseScale.y * giftFocusScale));
                float wingWidth = Mathf.Clamp(characterHeight * 1.28f, 0.72f, 2.8f);
                wingsRenderer.transform.localScale = Vector3.one * (width > 0f ? wingWidth / width : 1f);
            }
            decorationScale = giftFocused ? Mathf.Max(0.55f, lineupScale) : lineupScale;
            float identityScale = Mathf.Clamp(decorationScale, 0.72f, 0.88f);
            if (bannerRenderer.sprite != null)
            {
                float bannerWidth = bannerRenderer.sprite.bounds.size.x;
                bannerRenderer.transform.localScale = Vector3.one * (bannerWidth > 0f ? 1.58f * identityScale / bannerWidth : 1f);
            }
            float displayedCharacterHeight = Mathf.Max(0.1f, VisibleCharacterHeight(characterBaseScale.y * giftFocusScale));
            decorationBadgeY = displayedCharacterHeight + 0.22f;
            avatarRenderer.transform.localScale = avatarBaseScale * identityScale;
            nameLabel.transform.localScale = Vector3.one * identityScale;
            float titleScale = titleLabel.text.Length <= 9 ? 0.68f : titleLabel.text.Length <= 14 ? 0.59f : 0.5f;
            titleLabel.transform.localScale = Vector3.one * identityScale * titleScale;
            if (titleShadowLabel != null) titleShadowLabel.transform.localScale = titleLabel.transform.localScale;
            UpdateDecorationPositions(0f);
            wingsRenderer.enabled = decorated;
            bannerRenderer.enabled = decorated && bannerRenderer.sprite != null && !string.IsNullOrWhiteSpace(titleLabel.text);
        }

        private void UpdateDecorationPositions(float bounce)
        {
            if (wingsRenderer == null || bannerRenderer == null || wingAnchor == null || body == null || avatarRenderer == null || nameLabel == null || rankLabel == null || titleLabel == null || titleShadowLabel == null) return;
            float attachedBounce = bounce * 0.72f;
            float characterHeight = Mathf.Max(0.1f, VisibleCharacterHeight(characterBaseScale.y * giftFocusScale));
            wingAnchor.localPosition = new Vector3(0f, body.localPosition.y + characterHeight * 0.16f, 0.12f);
            wingAnchor.localRotation = body.localRotation;
            float identityScale = Mathf.Clamp(decorationScale, 0.72f, 0.88f);
            // The main camera faces toward negative Z, so positive world X appears
            // on the left side of the portrait frame.
            avatarRenderer.transform.localPosition = new Vector3(0.58f * identityScale, decorationBadgeY + attachedBounce, -0.01f);
            nameLabel.transform.localPosition = new Vector3(0.22f * identityScale, decorationBadgeY + attachedBounce, -0.02f);
            float bannerY = decorationBadgeY + 0.53f * identityScale + attachedBounce;
            bannerRenderer.transform.localPosition = new Vector3(0f, bannerY, -0.01f);
            float titleY = bannerY + bannerTextOffsetY * identityScale;
            titleLabel.transform.localPosition = new Vector3(0f, titleY, -0.02f);
            titleShadowLabel.transform.localPosition = new Vector3(-0.014f * identityScale, titleY - 0.018f * identityScale, -0.015f);
            rankLabel.transform.localPosition = new Vector3(0f, bannerY + 0.34f * identityScale, 0f);
        }

        public void ChangeCharacter()
        {
            (string nextName, Sprite[] frames) = CharacterLibrary.RandomCharacter(characterName);
            characterName = nextName;
            flipbook.SetFrames(frames, nextName == "hanhan_video_dance" ? 15f : Random.Range(10f, 14f));
            characterBaseScale = Vector3.one * CharacterScale() * GiftPowerScale() * lineupScale;
            body.localScale = characterBaseScale;
            Celebrate(1.2f);
        }

        private float CharacterScale()
        {
            Sprite sprite = characterRenderer.sprite;
            return sprite == null || sprite.bounds.size.y <= 0f ? 1f : 2.15f / sprite.bounds.size.y;
        }

        public void Dance(float seconds = 5f) => SetAction(ActionState.Dance, seconds);
        public void Jump(float seconds = 0.95f)
        {
            spawnDropPending = false;
            spawnDropping = false;
            transform.position = home;
            jumpStartedAt = Time.time;
            jumpDuration = Mathf.Clamp(seconds, 0.75f, 1.5f);
            SetAction(ActionState.Jump, jumpDuration);
        }
        public void Walk(float seconds = 5f)
        {
            spawnDropPending = false;
            spawnDropping = false;
            walkOrigin = new Vector3(transform.position.x, 0f, transform.position.z);
            transform.position = walkOrigin;
            walkStartedAt = Time.time;
            walkDuration = Mathf.Max(1f, seconds);
            SetAction(ActionState.Walk, walkDuration);
        }
        public void Celebrate(float seconds = 4f) => SetAction(ActionState.Celebrate, seconds);
        public void Grow(float seconds = 8f)
        {
            effectScale = 1.85f;
            growUntil = Time.time + Mathf.Max(2f, seconds);
            Celebrate(seconds);
        }

        public void SetDimmed(bool dimmed)
        {
            targetAlpha = dimmed ? 0.1f : 1f;
        }

        public void SetVisibility(float alpha)
        {
            targetAlpha = Mathf.Clamp01(alpha);
        }

        public void SetGiftFocus(bool focused)
        {
            giftFocused = focused;
            giftFocusScale = focused ? 1.08f : 1f;
            RefreshBadgeVisibility();
            if (focused && action != ActionState.Walk) Celebrate(3f);
        }

        public void ReturnToAssignedSlot()
        {
            transform.position = home;
        }

        private void SetAction(ActionState next, float seconds)
        {
            action = next;
            actionUntil = Time.time + seconds;
            Touch();
        }

        private void Update()
        {
            if (Time.time >= actionUntil && action != ActionState.Idle) action = ActionState.Idle;
            if (spawnDropping)
            {
                float progress = Mathf.Clamp01((Time.time - spawnDropStartedAt) / SpawnDropDuration);
                float heightOffset;
                if (progress < 0.72f)
                {
                    float fallProgress = progress / 0.72f;
                    heightOffset = SpawnDropHeight * (1f - fallProgress * fallProgress);
                }
                else
                {
                    float bounceProgress = (progress - 0.72f) / 0.28f;
                    heightOffset = Mathf.Sin(bounceProgress * Mathf.PI) * 0.28f * (1f - bounceProgress);
                }
                transform.position = home + Vector3.up * heightOffset;
                if (progress >= 1f)
                {
                    spawnDropping = false;
                    transform.position = home;
                }
            }
            else if (action == ActionState.Jump)
            {
                float progress = Mathf.Clamp01((Time.time - jumpStartedAt) / jumpDuration);
                float heightOffset;
                if (progress < 0.38f)
                {
                    float rise = progress / 0.38f;
                    float easedRise = rise * rise * (3f - 2f * rise);
                    heightOffset = JumpHeight * easedRise;
                }
                else if (progress < 0.82f)
                {
                    float fall = (progress - 0.38f) / 0.44f;
                    heightOffset = JumpHeight * (1f - fall * fall);
                }
                else
                {
                    float landing = (progress - 0.82f) / 0.18f;
                    heightOffset = Mathf.Sin(landing * Mathf.PI) * 0.16f * (1f - landing);
                }
                transform.position = home + Vector3.up * Mathf.Max(0f, heightOffset);
            }
            else if (action == ActionState.Walk)
            {
                float progress = Mathf.Clamp01((Time.time - walkStartedAt) / walkDuration);
                float smoothProgress = progress * progress * (3f - 2f * progress);
                float angle = smoothProgress * Mathf.PI * 2f;
                Vector3 circuitOffset = new(
                    Mathf.Sin(angle) * 4.6f,
                    0f,
                    Mathf.Sin(smoothProgress * Mathf.PI) * 3.6f
                );
                transform.position = walkOrigin + circuitOffset;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, home, 1f - Mathf.Exp(-5f * Time.deltaTime));
            }
            if (effectScale > 1f && Time.time >= growUntil) effectScale = Mathf.Lerp(effectScale, 1f, 5f * Time.deltaTime);
            visualAlpha = Mathf.Lerp(visualAlpha, targetAlpha, 1f - Mathf.Exp(-7f * Time.deltaTime));
            ApplyAlpha();

            float time = Time.time + phase;
            bool isPerforming = action is ActionState.Dance or ActionState.Jump or ActionState.Celebrate;
            flipbook.SetPlaybackSpeed(isPerforming ? 2.7f : 1.5f);
            float bounce = isPerforming ? Mathf.Abs(Mathf.Sin(time * 13.125f)) * 0.16f : 0f;
            body.localRotation = isPerforming
                ? Quaternion.Euler(0f, Mathf.Sin(time * 3.75f) * 5f, Mathf.Sin(time * 9.375f) * 2.5f)
                : Quaternion.identity;
            float pulse = action == ActionState.Celebrate ? 1f + Mathf.Sin(time * 16.875f) * 0.08f : 1f;
            Vector3 displayedScale = characterBaseScale * pulse * effectScale * giftFocusScale;
            body.localScale = displayedScale;
            float rawHeight = characterRenderer.sprite == null ? 0f : characterRenderer.sprite.bounds.size.y * displayedScale.y;
            float groundOffset = characterRenderer.sprite == null
                ? 0f
                : -characterRenderer.sprite.bounds.min.y * displayedScale.y - rawHeight * BottomPaddingFraction();
            body.localPosition = Vector3.up * (groundOffset + bounce);
            UpdateDecorationPositions(bounce);

            Camera camera = Camera.main;
            if (camera != null)
            {
                nameLabel.transform.rotation = camera.transform.rotation;
                rankLabel.transform.rotation = camera.transform.rotation;
                titleLabel.transform.rotation = camera.transform.rotation;
                titleShadowLabel.transform.rotation = camera.transform.rotation;
                avatarRenderer.transform.rotation = camera.transform.rotation;
                bannerRenderer.transform.rotation = camera.transform.rotation;
            }
        }

        private void ApplyAlpha()
        {
            if (characterRenderer != null)
            {
                Color color = characterRenderer.color;
                color.a = visualAlpha;
                characterRenderer.color = color;
            }
            if (avatarRenderer != null)
            {
                Color color = avatarRenderer.color;
                // NPCs are only placeholders, so do not present their generated
                // portraits as real viewer avatars. Real TikTok users keep the
                // normal visibility/focus alpha used by the rest of the actor.
                color.a = UsesSyntheticAvatar ? 0f : visualAlpha;
                avatarRenderer.color = color;
            }
            if (wingsRenderer != null)
            {
                Color color = wingsRenderer.color;
                color.a = visualAlpha;
                wingsRenderer.color = color;
            }
            if (bannerRenderer != null)
            {
                Color color = bannerRenderer.color;
                color.a = visualAlpha;
                bannerRenderer.color = color;
            }
            SetTextAlpha(nameLabel, visualAlpha);
            SetTextAlpha(rankLabel, visualAlpha);
            SetTextAlpha(titleLabel, visualAlpha);
            SetTextAlpha(titleShadowLabel, visualAlpha * 0.92f);
        }

        private static void SetTextAlpha(TextMesh label, float alpha)
        {
            if (label == null) return;
            Color color = label.color;
            color.a = alpha;
            label.color = color;
        }

        private float VisibleCharacterHeight(float scale)
        {
            if (characterRenderer.sprite == null) return 0f;
            return characterRenderer.sprite.bounds.size.y * scale * (1f - BottomPaddingFraction());
        }

        private float BottomPaddingFraction()
        {
            return characterName switch
            {
                "a" => 0.004f,
                "b" => 0.036f,
                "c" => 0f,
                "d" => 0f,
                "e" => 0.029f,
                "g" => 0f,
                "h" => 0.188f,
                "hanhan_video_dance" => 0.151f,
                "j" => 0.071f,
                "k" => 0.035f,
                "mushroom_dance_01" => 0.038f,
                "mushroom_dance_15" => 0.046f,
                "mushroom_magic_02" => 0.021f,
                _ => 0f
            };
        }

        private enum ActionState { Idle, Dance, Jump, Walk, Celebrate }
    }
}
