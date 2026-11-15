-- Tighten review identity and remove public RPC access to the RLS event function.
ALTER TABLE public.mall_reviews
    DROP CONSTRAINT mall_reviews_userid_itemid_key;

ALTER TABLE public.mall_reviews
    ADD CONSTRAINT mall_reviews_pkey PRIMARY KEY (userid, itemid);

REVOKE EXECUTE ON FUNCTION public.rls_auto_enable()
    FROM PUBLIC, anon, authenticated, service_role;