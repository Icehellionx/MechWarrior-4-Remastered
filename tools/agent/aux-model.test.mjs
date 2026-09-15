import assert from 'node:assert/strict';
import test from 'node:test';
import { AUX_ROUTER_VERSION, callAuxiliaryModel, loadAuxiliaryConfig, selectAuxiliaryModel } from './aux-model.mjs';

const base = {
  LOCAL_MODEL_ENABLED: 'true', LOCAL_MODEL_BASE_URL: 'http://127.0.0.1:11434',
  LOCAL_ADVISER_MODEL: 'local-adviser', FEATHERLESS_API_KEY: 'test-key',
  FEATHERLESS_ADVISER_MODEL: 'remote-adviser', FEATHERLESS_FAST_MODEL: 'remote-fast',
};

test('router version is pinned', () => assert.equal(AUX_ROUTER_VERSION, '1.0.0'));
test('auto routing is local-first', () => {
  const selected = selectAuxiliaryModel(loadAuxiliaryConfig(base), 'adviser', 'auto');
  assert.equal(selected.route, 'local');
  assert.equal(selected.model, 'local-adviser');
});
test('explicit remote request keeps credentials in the header and disables thinking', async () => {
  let captured;
  const answer = await callAuxiliaryModel(loadAuxiliaryConfig(base), { role: 'adviser', route: 'remote', prompt: 'Review.', maximumTokens: 128 }, {
    fetchImpl: async (url, options) => { captured = { url, options }; return new Response(JSON.stringify({ choices: [{ message: { content: 'Advice.' } }] }), { status: 200 }); },
  });
  assert.equal(answer.content, 'Advice.');
  assert.equal(captured.options.headers.Authorization, 'Bearer test-key');
  assert.doesNotMatch(captured.options.body, /test-key/);
  assert.deepEqual(JSON.parse(captured.options.body).chat_template_kwargs, { enable_thinking: false });
});
test('unsafe provider endpoints and thinking traces fail closed', async () => {
  assert.throws(() => loadAuxiliaryConfig({ ...base, LOCAL_MODEL_BASE_URL: 'https://example.com' }), /loopback/);
  await assert.rejects(() => callAuxiliaryModel(loadAuxiliaryConfig(base), { role: 'adviser', route: 'remote', prompt: 'Review.' }, {
    fetchImpl: async () => new Response(JSON.stringify({ choices: [{ message: { content: '<think>hidden</think>' } }] }), { status: 200 }),
  }), /thinking trace/);
});
